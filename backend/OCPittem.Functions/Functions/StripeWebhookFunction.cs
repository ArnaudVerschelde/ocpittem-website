using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using Stripe;
using Stripe.Checkout;

namespace OCPittem.Functions.Functions;

public class StripeWebhookFunction
{
    private readonly IStripeService _stripe;
    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly ITicketPdfService _ticketPdf;
    private readonly AppOptions _appOptions;
    private readonly ILogger<StripeWebhookFunction> _logger;

    public StripeWebhookFunction(
        IStripeService stripe,
        IStorageService storage,
        IEmailService email,
        ITicketPdfService ticketPdf,
        IOptions<AppOptions> appOptions,
        ILogger<StripeWebhookFunction> logger)
    {
        _stripe = stripe;
        _storage = storage;
        _email = email;
        _ticketPdf = ticketPdf;
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

        // Idempotency check
        if (await _storage.WebhookEventExistsAsync(stripeEvent.Id))
        {
            _logger.LogInformation("Webhook event {EventId} already processed, skipping", stripeEvent.Id);
            return new OkResult();
        }

        // Record the event
        var webhookEntity = new WebhookEventEntity
        {
            PartitionKey = "Stripe",
            RowKey = stripeEvent.Id,
            ReceivedAt = DateTime.UtcNow,
            Result = "received",
        };
        await _storage.SaveWebhookEventAsync(webhookEntity);

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

        var orderType = session.Metadata.GetValueOrDefault("orderType") ?? "ticket";
        if (orderType == "sponsor")
            await HandleSponsorCheckoutCompleted(session);
        else
            await HandleTicketCheckoutCompleted(session);
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

        await _storage.UpdateSponsorRequestAsync(sponsor);

        if (!string.IsNullOrEmpty(email))
        {
            await _email.SendSponsorPaymentConfirmationAsync(
                email, companyName, sponsor.Package,
                sponsor.ExtraEtenPartyCount, sponsor.ExtraVegetarischCount, sponsor.ExtraDrankkaart20Count,
                sponsor.IncludedVegetarischCount,
                pdfTickets, combinedPdf);
            _logger.LogInformation("Sponsor payment confirmation sent to {Email} for request {RequestId}", email, requestId);
        }

        var contactEmail = string.IsNullOrEmpty(_appOptions.ContactEmail)
            ? "oudercomitepittem@gmail.com"
            : _appOptions.ContactEmail;
        await _email.SendContactNotificationAsync(
            companyName, email,
            $"Sponsorpakket betaald: {sponsor.Package} \u2014 {companyName}",
            $"Bedrijf: {companyName}\nOndernemingsnummer: {sponsor.EnterpriseNumber}\nContactpersoon: {sponsor.ContactName}\nPakket: {sponsor.Package}\nExtra Eten & Party: {sponsor.ExtraEtenPartyCount}\nExtra Drankkaarten \u20ac20: {sponsor.ExtraDrankkaart20Count}\n\n{sponsor.Message}",
            contactEmail);
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Session session) return;

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

    private string GenerateQrPayload(string ticketId)
    {
        var secret = Encoding.UTF8.GetBytes(_appOptions.TicketHmacSecret);
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticketId));
        var signature = Convert.ToBase64String(hash)[..16];
        return $"{ticketId}:{signature}";
    }
}
