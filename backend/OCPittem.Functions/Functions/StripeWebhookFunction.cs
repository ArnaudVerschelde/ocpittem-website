using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using OCPittem.Functions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace OCPittem.Functions.Functions;

public class StripeWebhookFunction
{
    private readonly IStripeService _stripe;
    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly ITicketPdfService _ticketPdf;
    private readonly ISponsorAttestationService _attestation;
    private readonly AppOptions _appOptions;
    private readonly ILogger<StripeWebhookFunction> _logger;

    public StripeWebhookFunction(
        IStripeService stripe,
        IStorageService storage,
        IEmailService email,
        ITicketPdfService ticketPdf,
        ISponsorAttestationService attestation,
        IOptions<AppOptions> appOptions,
        ILogger<StripeWebhookFunction> logger)
    {
        _stripe = stripe;
        _storage = storage;
        _email = email;
        _ticketPdf = ticketPdf;
        _attestation = attestation;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    [Function("StripeWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stripe/webhook")] HttpRequest req)
    {
        string json;
        using (var reader = new StreamReader(req.Body))
        {
            json = await reader.ReadToEndAsync();
        }

        Event stripeEvent;
        try
        {
            var signature = req.Headers["Stripe-Signature"].ToString();
            stripeEvent = _stripe.ConstructWebhookEvent(json, signature);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return new BadRequestObjectResult(new { error = "Invalid signature." });
        }

        var isCookieSaleEvent = stripeEvent.Data.Object is Session eventSession
            && eventSession.Metadata?.GetValueOrDefault("flow") == CookieSale2026Catalog.Flow;
        var beginResult = await _storage.TryBeginWebhookEventAsync(
            stripeEvent.Id,
            allowRetry: isCookieSaleEvent,
            DateTimeOffset.UtcNow);

        if (beginResult == WebhookEventBeginResult.AlreadyProcessed)
        {
            _logger.LogInformation("Webhook event {EventId} already processed, skipping", stripeEvent.Id);
            return new OkResult();
        }

        if (beginResult == WebhookEventBeginResult.InProgress)
        {
            _logger.LogInformation("Webhook event {EventId} is already being processed", stripeEvent.Id);
            return isCookieSaleEvent
                ? new ObjectResult(new { error = "Webhook event is still processing." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                }
                : new OkResult();
        }

        var webhookEntity = new WebhookEventEntity
        {
            PartitionKey = "Stripe",
            RowKey = stripeEvent.Id,
            ReceivedAt = DateTime.UtcNow,
            Result = "received",
        };
        try
        {
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    await HandleCheckoutCompleted(stripeEvent);
                    break;

                case EventTypes.CheckoutSessionAsyncPaymentSucceeded:
                    await HandleCheckoutCompleted(stripeEvent);
                    break;

                case EventTypes.CheckoutSessionAsyncPaymentFailed:
                    await HandlePaymentFailed(stripeEvent);
                    break;

                case EventTypes.CheckoutSessionExpired:
                    await HandleSessionExpired(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                    break;
            }

            webhookEntity.ProcessedAt = DateTime.UtcNow;
            webhookEntity.Result = "processed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventId}", stripeEvent.Id);
            webhookEntity.ProcessedAt = DateTime.UtcNow;
            webhookEntity.Result = $"error: {ex.Message}";
            await _storage.UpsertWebhookEventAsync(webhookEntity);
            if (isCookieSaleEvent)
            {
                return new ObjectResult(new { error = "Webhook processing failed." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                };
            }

            return new OkResult();
        }

        await _storage.UpsertWebhookEventAsync(webhookEntity);
        return new OkResult();
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Session session)
        {
            _logger.LogWarning("Could not cast event data to Session");
            return;
        }

        if (session.PaymentStatus != "paid")
        {
            _logger.LogInformation("Session {SessionId} payment status is {Status}, skipping",
                session.Id, session.PaymentStatus);
            return;
        }

        var flow = session.Metadata.GetValueOrDefault("flow");
        if (flow == CookieSale2026Catalog.Flow)
        {
            await HandleCookieSaleCheckoutCompleted(session);
            return;
        }

        var orderType = session.Metadata.GetValueOrDefault("orderType") ?? "ticket";
        if (orderType == "sponsor")
            await HandleSponsorCheckoutCompleted(session);
        else
            await HandleTicketCheckoutCompleted(session);
    }

    private async Task HandleCookieSaleCheckoutCompleted(Session session)
    {
        var orderId = session.Metadata.GetValueOrDefault("orderId") ?? "";
        if (string.IsNullOrWhiteSpace(orderId))
            throw new InvalidOperationException("Cookie checkout session has no orderId metadata.");

        var order = await _storage.GetCookieOrderAsync(orderId)
            ?? throw new InvalidOperationException($"Cookie order {orderId} was not found.");

        if (session.AmountTotal != order.TotalAmountCents
            || !string.Equals(session.Currency, "eur", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Paid amount mismatch for cookie order {orderId}.");
        }

        var paidOrder = await _storage.TryMarkCookieOrderPaidAsync(
            orderId,
            session.Id,
            session.PaymentIntentId ?? string.Empty,
            DateTimeOffset.UtcNow);

        if (paidOrder is null)
        {
            _logger.LogWarning(
                "Cookie order {OrderId} could not transition to Paid from its current status",
                orderId);
            return;
        }

        var claimedUtc = DateTimeOffset.UtcNow;
        var claimId = Guid.NewGuid().ToString();
        var claimedOrder = await _storage.TryClaimCookieOrderConfirmationEmailAsync(
            orderId,
            claimId,
            claimedUtc);
        if (claimedOrder is null)
            return;

        try
        {
            await _email.SendCookieSaleConfirmationAsync(new CookieSaleConfirmationData(
                claimedOrder.OrderId,
                claimedOrder.ConfirmationNumber,
                claimedOrder.Name,
                claimedOrder.Email,
                claimedOrder.ClassName,
                claimedOrder.CoteDorQuantity,
                claimedOrder.LotusQuantity,
                claimedOrder.TotalPackages,
                claimedOrder.TotalAmountCents));
            await _storage.MarkCookieOrderConfirmationEmailSentAsync(
                orderId,
                claimId,
                DateTimeOffset.UtcNow);
            _logger.LogInformation(
                "Cookie order {OrderId} marked Paid and confirmation sent ({ConfirmationNumber})",
                orderId,
                claimedOrder.ConfirmationNumber);
        }
        catch
        {
            await _storage.ReleaseCookieOrderConfirmationEmailClaimAsync(orderId, claimId);
            throw;
        }
    }

    private async Task HandleTicketCheckoutCompleted(Session session)
    {
        var orderId = session.Metadata.GetValueOrDefault("orderId") ?? "";

        if (string.IsNullOrEmpty(orderId))
        {
            _logger.LogWarning("No orderId in session metadata for session {SessionId}", session.Id);
            return;
        }

        var order = await _storage.GetOrderByStripeSessionAsync(session.Id);
        if (order == null)
        {
            _logger.LogWarning("Order not found for Stripe session {SessionId}. Cannot generate tickets.", session.Id);
            return;
        }

        order.Status = nameof(OrderStatus.Paid);
        order.StripeSessionId = session.Id;

        var toegangsticketCount = order.ToegangsticketCount;
        var etenPartyCount = order.EtenPartyCount;
        var vegetarischCount = order.VegetarischCount;
        var email = session.CustomerEmail ?? order.Email ?? "";
        var customerName = session.Metadata.GetValueOrDefault("customerName") ?? order.Name ?? "";

        var pdfTickets = new List<TicketPdfData>();

        for (int i = 0; i < toegangsticketCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = GenerateQrPayload(ticketId);

            var ticket = new TicketEntity
            {
                PartitionKey = orderId,
                RowKey = ticketId,
                QrPayload = qrPayload,
                TicketType = nameof(TicketKind.Toegang),
                IsVegetarisch = false,
            };

            await _storage.SaveTicketAsync(ticket);
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.Toegang), false));
        }

        for (int i = 0; i < etenPartyCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = GenerateQrPayload(ticketId);
            var isVeg = i < vegetarischCount;

            var ticket = new TicketEntity
            {
                PartitionKey = orderId,
                RowKey = ticketId,
                QrPayload = qrPayload,
                TicketType = nameof(TicketKind.EtenParty),
                IsVegetarisch = isVeg,
            };

            await _storage.SaveTicketAsync(ticket);
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        byte[]? combinedPdf = pdfTickets.Count > 0
            ? _ticketPdf.GenerateTicketsPdf(pdfTickets, customerName, "Bal Parental 2026")
            : null;

        if (combinedPdf != null)
        {
            var blobUrl = await _storage.SaveTicketPdfAsync(orderId, combinedPdf);
            order.PdfBlobUrl = blobUrl;
            _logger.LogInformation("Ticket PDF saved to blob for order {OrderId}", orderId);
        }

        await _storage.UpdateOrderAsync(order);

        if (!string.IsNullOrEmpty(email))
        {
            await _email.SendTicketConfirmationAsync(
                email,
                customerName,
                toegangsticketCount,
                etenPartyCount,
                vegetarischCount,
                order.Drankkaart10Count,
                order.Drankkaart20Count,
                pdfTickets,
                combinedPdf);
            _logger.LogInformation("Ticket confirmation email sent to {Email} for order {OrderId}", email, orderId);
        }
    }

    private async Task HandleSponsorCheckoutCompleted(Session session)
    {
        var requestId = session.Metadata.GetValueOrDefault("requestId") ?? "";
        if (string.IsNullOrEmpty(requestId))
        {
            _logger.LogWarning("No requestId in sponsor session metadata for session {SessionId}", session.Id);
            return;
        }

        var sponsor = await _storage.GetSponsorRequestByStripeSessionAsync(session.Id);
        if (sponsor == null)
        {
            _logger.LogWarning("Sponsor request not found for Stripe session {SessionId}.", session.Id);
            return;
        }

        sponsor.Status = "Paid";
        var email = session.CustomerEmail ?? sponsor.Email;
        var companyName = session.Metadata.GetValueOrDefault("customerName") ?? sponsor.CompanyName;

        var includedTickets = sponsor.Package.ToLower() switch
        {
            "zilver" => 2, "goud" => 4, _ => 0
        };

        var pdfTickets = new List<TicketPdfData>();

        for (int i = 0; i < includedTickets; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = GenerateQrPayload(ticketId);
            var isVeg = i < sponsor.IncludedVegetarischCount;
            var ticket = new TicketEntity
            {
                PartitionKey = requestId, RowKey = ticketId,
                QrPayload = qrPayload, TicketType = nameof(TicketKind.EtenParty), IsVegetarisch = isVeg,
            };
            await _storage.SaveTicketAsync(ticket);
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        for (int i = 0; i < sponsor.ExtraEtenPartyCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = GenerateQrPayload(ticketId);
            var isVeg = i < sponsor.ExtraVegetarischCount;
            var ticket = new TicketEntity
            {
                PartitionKey = requestId, RowKey = ticketId,
                QrPayload = qrPayload, TicketType = nameof(TicketKind.EtenParty), IsVegetarisch = isVeg,
            };
            await _storage.SaveTicketAsync(ticket);
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        byte[]? combinedPdf = pdfTickets.Count > 0
            ? _ticketPdf.GenerateTicketsPdf(pdfTickets, companyName, "Bal Parental 2026")
            : null;

        if (combinedPdf != null)
        {
            var blobUrl = await _storage.SaveTicketPdfAsync(requestId, combinedPdf);
            sponsor.PdfBlobUrl = blobUrl;
            _logger.LogInformation("Sponsor PDF saved to blob for request {RequestId}", requestId);
        }

        var packagePrice = sponsor.Package.ToLower() switch { "brons" => 100m, "zilver" => 250m, "goud" => 500m, _ => 0m };
        var total = packagePrice + sponsor.ExtraEtenPartyCount * 50m + sponsor.ExtraDrankkaart20Count * 20m;

        byte[]? attestPdf = null;
        try
        {
            attestPdf = await _attestation.GenerateAttestationAsync(
                sponsor.CompanyName, sponsor.Street, sponsor.HouseNumber,
                sponsor.PostalCode, sponsor.City, sponsor.EnterpriseNumber,
                total, DateTime.UtcNow);
            var attestUrl = await _storage.SaveSponsorAttestationAsync(requestId, attestPdf);
            sponsor.AttestationBlobUrl = attestUrl;
            _logger.LogInformation("Sponsor attestation saved to blob for request {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate/save attestation for request {RequestId}", requestId);
        }

        await _storage.UpdateSponsorRequestAsync(sponsor);

        if (!string.IsNullOrEmpty(email))
        {
            await _email.SendSponsorPaymentConfirmationAsync(
                email, companyName, sponsor.Package,
                sponsor.ExtraEtenPartyCount, sponsor.ExtraVegetarischCount, sponsor.ExtraDrankkaart20Count,
                sponsor.IncludedVegetarischCount,
                pdfTickets, combinedPdf, attestPdf);
            _logger.LogInformation("Sponsor payment confirmation sent to {Email} for request {RequestId}", email, requestId);
        }

        var contactEmail = string.IsNullOrEmpty(_appOptions.ContactEmail)
            ? "oudercomitepittem@gmail.com"
            : _appOptions.ContactEmail;
        await _email.SendContactNotificationAsync(
            companyName, email,
            $"Sponsorpakket betaald: {sponsor.Package} \u2014 {companyName}",
            $"Bedrijf: {companyName}\nOndernemingsnummer: {sponsor.EnterpriseNumber}\nAdres: {sponsor.Street} {sponsor.HouseNumber}, {sponsor.PostalCode} {sponsor.City}\nContactpersoon: {sponsor.ContactName}\nPakket: {sponsor.Package}\nExtra Eten & Party: {sponsor.ExtraEtenPartyCount}\nExtra Drankkaarten \u20ac20: {sponsor.ExtraDrankkaart20Count}\nSponsor aanwezig: {(sponsor.SponsorAttends ? "Ja" : "Nee")}\nAantal aanwezigen: {sponsor.SponsorAttendeesCount}",
            contactEmail);
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Session session) return;

        var flow = session.Metadata.GetValueOrDefault("flow");
        if (flow == CookieSale2026Catalog.Flow)
        {
            var cookieOrderId = session.Metadata.GetValueOrDefault("orderId") ?? "";
            if (!string.IsNullOrWhiteSpace(cookieOrderId))
                await _storage.TryMarkCookieOrderStatusAsync(cookieOrderId, CookieOrderStatus.Failed);
            return;
        }

        var orderType = session.Metadata.GetValueOrDefault("orderType") ?? "ticket";
        _logger.LogWarning("Payment failed for session {SessionId}, type {OrderType}", session.Id, orderType);

        if (orderType == "sponsor")
        {
            var sponsor = await _storage.GetSponsorRequestByStripeSessionAsync(session.Id);
            if (sponsor != null)
            {
                sponsor.Status = "Failed";
                await _storage.UpdateSponsorRequestAsync(sponsor);
            }
        }
        else
        {
            var order = await _storage.GetOrderByStripeSessionAsync(session.Id);
            if (order != null)
            {
                order.Status = nameof(OrderStatus.Failed);
                await _storage.UpdateOrderAsync(order);
            }
        }
    }

    private async Task HandleSessionExpired(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Session session)
            return;

        if (session.Metadata.GetValueOrDefault("flow") != CookieSale2026Catalog.Flow)
            return;

        var orderId = session.Metadata.GetValueOrDefault("orderId") ?? "";
        if (!string.IsNullOrWhiteSpace(orderId))
            await _storage.TryMarkCookieOrderStatusAsync(orderId, CookieOrderStatus.Cancelled);
    }

    private string GenerateQrPayload(string ticketId)
        => QrPayloadHelper.Generate(ticketId, _appOptions.TicketHmacSecret);
}
