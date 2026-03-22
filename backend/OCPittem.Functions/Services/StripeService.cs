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
    private readonly string _priceIdSponsorBrons;
    private readonly string _priceIdSponsorZilver;
    private readonly string _priceIdSponsorGoud;
    private readonly string _frontendUrl;

    public StripeService(StripeOptions options, string frontendUrl)
    {
        StripeConfiguration.ApiKey = options.SecretKey;
        _webhookSecret = options.WebhookSecret;
        _priceIdToegangsticket = options.PriceIdToegangsticket;
        _priceIdEtenParty = options.PriceIdEtenParty;
        _priceIdDrankkaart10 = options.PriceIdDrankkaart10;
        _priceIdDrankkaart20 = options.PriceIdDrankkaart20;
        _priceIdSponsorBrons = options.PriceIdSponsorBrons;
        _priceIdSponsorZilver = options.PriceIdSponsorZilver;
        _priceIdSponsorGoud = options.PriceIdSponsorGoud;
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

    public async Task<StripeCheckoutResult> CreateSponsorCheckoutSessionAsync(
        string requestId, string email, string companyName,
        string packageName, int extraEtenPartyCount, int extraDrankkaart20Count)
    {
        var packagePriceId = packageName.ToLower() switch
        {
            "brons" => _priceIdSponsorBrons,
            "zilver" => _priceIdSponsorZilver,
            "goud" => _priceIdSponsorGoud,
            _ => throw new ArgumentException($"Unknown sponsor package: {packageName}")
        };

        var lineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions { Price = packagePriceId, Quantity = 1 }
        };

        if (extraEtenPartyCount > 0)
            lineItems.Add(new SessionLineItemOptions { Price = _priceIdEtenParty, Quantity = extraEtenPartyCount });
        if (extraDrankkaart20Count > 0)
            lineItems.Add(new SessionLineItemOptions { Price = _priceIdDrankkaart20, Quantity = extraDrankkaart20Count });

        var sponsorOptions = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card", "bancontact", "ideal"],
            CustomerEmail = email,
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = $"{_frontendUrl}/betaling/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{_frontendUrl}/betaling/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "requestId", requestId },
                { "customerName", companyName },
                { "orderType", "sponsor" },
            },
        };

        var sponsorService = new SessionService();
        var sponsorSession = await sponsorService.CreateAsync(sponsorOptions);
        return new StripeCheckoutResult(sponsorSession.Url!, sponsorSession.Id);
    }

    public Stripe.Event ConstructWebhookEvent(string json, string signature)
        => EventUtility.ConstructEvent(json, signature, _webhookSecret,
            tolerance: 600, throwOnApiVersionMismatch: false);
}
