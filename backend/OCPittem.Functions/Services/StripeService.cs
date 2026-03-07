using Stripe;
using Stripe.Checkout;

namespace OCPittem.Functions.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(string orderId, string email, string name, int quantity);
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

    public async Task<string> CreateCheckoutSessionAsync(string orderId, string email, string name, int quantity)
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
            SuccessUrl = $"{_frontendUrl}/bal-parental?success=true&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_frontendUrl}/bal-parental?canceled=true",
            Metadata = new Dictionary<string, string>
            {
                { "orderId", orderId },
                { "customerName", name },
            },
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public Stripe.Event ConstructWebhookEvent(string json, string signature)
    {
        return EventUtility.ConstructEvent(json, signature, _webhookSecret);
    }
}
