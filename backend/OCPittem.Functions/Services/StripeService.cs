using Stripe;
using Stripe.Checkout;

namespace OCPittem.Functions.Services;

public record StripeCheckoutResult(string Url, string SessionId);

public class StripeService : IStripeService
{
    private readonly string _webhookSecret;
    private readonly string _priceIdToegangsticket;
    private readonly string _priceIdEtenParty;
    private readonly string _priceIdDrankkaart10;
    private readonly string _priceIdDrankkaart20;
    private readonly string _frontendUrl;

    public StripeService(StripeOptions options, string frontendUrl)
    {
        StripeConfiguration.ApiKey = options.SecretKey;
        _webhookSecret = options.WebhookSecret;
        _priceIdToegangsticket = options.PriceIdToegangsticket;
        _priceIdEtenParty = options.PriceIdEtenParty;
        _priceIdDrankkaart10 = options.PriceIdDrankkaart10;
        _priceIdDrankkaart20 = options.PriceIdDrankkaart20;
        _frontendUrl = frontendUrl;
    }

    public async Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
        string orderId, string email, string name,
        int toegangsticketCount, int etenPartyCount,
        int drankkaart10Count, int drankkaart20Count)
    {
        var lineItems = new List<SessionLineItemOptions>();

        if (toegangsticketCount > 0)
            lineItems.Add(new SessionLineItemOptions { Price = _priceIdToegangsticket, Quantity = toegangsticketCount });
        if (etenPartyCount > 0)
            lineItems.Add(new SessionLineItemOptions { Price = _priceIdEtenParty, Quantity = etenPartyCount });
        if (drankkaart10Count > 0)
            lineItems.Add(new SessionLineItemOptions { Price = _priceIdDrankkaart10, Quantity = drankkaart10Count });
        if (drankkaart20Count > 0)
            lineItems.Add(new SessionLineItemOptions { Price = _priceIdDrankkaart20, Quantity = drankkaart20Count });

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card", "bancontact", "ideal"],
            CustomerEmail = email,
            LineItems = lineItems,
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
        return new StripeCheckoutResult(session.Url!, session.Id);
    }

    public Stripe.Event ConstructWebhookEvent(string json, string signature)
        => EventUtility.ConstructEvent(json, signature, _webhookSecret,
            tolerance: 600, throwOnApiVersionMismatch: false);
}
