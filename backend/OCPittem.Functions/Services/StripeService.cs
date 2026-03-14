using Stripe;
using Stripe.Checkout;

namespace OCPittem.Functions.Services;

public record StripeCheckoutResult(string Url, string SessionId);

public interface IStripeService
{
    Task<StripeCheckoutResult> CreateCheckoutSessionAsync(string orderId, string email, string name, int quantity);
    Stripe.Event ConstructWebhookEvent(string json, string signature);
}

public class StripeService : IStripeService
{
    private readonly string _webhookSecret;
    private readonly string _ticketPriceId;
    private readonly string _frontendUrl;

    public StripeService(string secretKey, string webhookSecret, string ticketPriceId, string frontendUrl)
    {
        StripeConfiguration.ApiKey = secretKey;
        _webhookSecret = webhookSecret;
        _ticketPriceId = ticketPriceId;
        _frontendUrl = frontendUrl;
    }

    public async Task<StripeCheckoutResult> CreateCheckoutSessionAsync(string orderId, string email, string name, int quantity)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card", "bancontact", "ideal"],
            CustomerEmail = email,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = _ticketPriceId,
                    Quantity = quantity,
                }
            ],
            Mode = "payment",
            SuccessUrl = $"{_frontendUrl}/betaling/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_frontendUrl}/betaling/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "orderId", orderId },
                { "customerName", name },
            },
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return new StripeCheckoutResult(session.Url, session.Id);
    }

    public Stripe.Event ConstructWebhookEvent(string json, string signature)
    {
        return EventUtility.ConstructEvent(
            json,
            signature,
            _webhookSecret,
            tolerance: 600,
            throwOnApiVersionMismatch: false
        );
    }
}
