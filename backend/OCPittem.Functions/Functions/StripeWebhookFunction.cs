using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<StripeWebhookFunction> _logger;

    public StripeWebhookFunction(
        IStripeService stripe,
        IStorageService storage,
        IEmailService email,
        ITicketPdfService ticketPdf,
        ILogger<StripeWebhookFunction> logger)
    {
        _stripe = stripe;
        _storage = storage;
        _email = email;
        _ticketPdf = ticketPdf;
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
            _logger.LogInformation("Session {SessionId} payment status is {Status}, skipping ticket generation",
                session.Id, session.PaymentStatus);
            return;
        }

        var orderId = session.Metadata.GetValueOrDefault("orderId") ?? "";
        var customerName = session.Metadata.GetValueOrDefault("customerName") ?? "";

        if (string.IsNullOrEmpty(orderId))
        {
            _logger.LogWarning("No orderId in session metadata for session {SessionId}", session.Id);
            return;
        }

        // Update order
        var order = await _storage.GetOrderByStripeSessionAsync(session.Id);
        if (order == null)
        {
            // Order may have been stored without StripeSessionId — try finding by orderId
            _logger.LogInformation("Order not found by session, creating tickets for order {OrderId}", orderId);
        }
        else
        {
            order.Status = "paid";
            order.StripeSessionId = session.Id;
            await _storage.UpdateOrderAsync(order);
        }

        // Generate tickets
        var quantity = (int)(session.AmountTotal.GetValueOrDefault() > 0
            ? session.LineItems?.Data?.Sum(li => li.Quantity ?? 0) ?? 1
            : 1);

        // Use order quantity if available
        if (order != null) quantity = order.Quantity;

        var pdfPages = new List<byte[]>();

        for (int i = 0; i < quantity; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = GenerateQrPayload(ticketId);

            var ticket = new TicketEntity
            {
                PartitionKey = orderId,
                RowKey = ticketId,
                QrPayload = qrPayload,
            };

            await _storage.SaveTicketAsync(ticket);

            var pdf = _ticketPdf.GenerateTicketPdf(ticketId, customerName, "Bal Parental 2026", qrPayload);
            pdfPages.Add(pdf);
        }

        // Send email with first ticket PDF (in production, merge PDFs or attach all)
        var combinedPdf = pdfPages.FirstOrDefault();
        var email = session.CustomerEmail ?? order?.Email ?? "";

        if (!string.IsNullOrEmpty(email))
        {
            await _email.SendTicketConfirmationAsync(email, customerName, quantity, combinedPdf);
            _logger.LogInformation("Ticket confirmation email sent to {Email} for order {OrderId}", email, orderId);
        }
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is not Session session) return;

        var orderId = session.Metadata.GetValueOrDefault("orderId") ?? "";
        _logger.LogWarning("Payment failed for session {SessionId}, order {OrderId}", session.Id, orderId);

        var order = await _storage.GetOrderByStripeSessionAsync(session.Id);
        if (order != null)
        {
            order.Status = "failed";
            await _storage.UpdateOrderAsync(order);
        }
    }

    private static string GenerateQrPayload(string ticketId)
    {
        // HMAC signature for QR validation
        // In production, use a proper secret from configuration
        var secret = Encoding.UTF8.GetBytes("ocpittem-ticket-secret-change-me");
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticketId));
        var signature = Convert.ToBase64String(hash)[..16];
        return $"{ticketId}:{signature}";
    }
}
