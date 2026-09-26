using OCPittem.Functions.Services;
using Stripe;

namespace OCPittem.Functions.Tests.Services;

public class StripeServiceTests
{
    private static StripeService CreateSut() => new(
        new StripeOptions
        {
            SecretKey = "sk_test_fake",
            WebhookSecret = "whsec_test_fake_secret_that_is_long_enough",
            PriceIdToegangsticket = "price_toegang",
            PriceIdEtenParty = "price_eten",
            PriceIdDrankkaart10 = "price_drank10",
            PriceIdDrankkaart20 = "price_drank20",
            PriceIdSponsorBrons = "price_brons",
            PriceIdSponsorZilver = "price_zilver",
            PriceIdSponsorGoud = "price_goud",
            PriceIdCookieCoteDor = "price_cookie_cotedor",
            PriceIdCookieLotus = "price_cookie_lotus",
        },
        frontendUrl: "http://localhost:5173");

    [Fact]
    public void ConstructWebhookEvent_InvalidSignature_ThrowsStripeException()
    {
        var sut = CreateSut();

        Assert.Throws<StripeException>(() =>
            sut.ConstructWebhookEvent("{}", "t=1,v1=invalidsignature"));
    }

    [Fact]
    public async Task CreateSponsorCheckoutSessionAsync_UnknownPackage_ThrowsArgumentException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.CreateSponsorCheckoutSessionAsync("req-1", "bedrijf@example.com", "Bedrijf NV", "platinum", 0, 0));
    }

    [Theory]
    [InlineData("brons")]
    [InlineData("zilver")]
    [InlineData("goud")]
    [InlineData("BRONS")]
    [InlineData("GOUD")]
    public async Task CreateSponsorCheckoutSessionAsync_UnknownPackage_OnlyThrowsForUnknown(string knownPackage)
    {
        // Valid package names should NOT throw ArgumentException (they may fail on Stripe API, which is OK)
        var sut = CreateSut();

        var ex = await Record.ExceptionAsync(() =>
            sut.CreateSponsorCheckoutSessionAsync("req-1", "bedrijf@example.com", "Bedrijf NV", knownPackage, 0, 0));

        Assert.IsNotType<ArgumentException>(ex);
    }

    [Fact]
    public void BuildCookieSaleCheckoutOptions_UsesConfiguredPricesAndMinimalMetadata()
    {
        var sut = CreateSut();

        var options = sut.BuildCookieSaleCheckoutOptions(
            "order-123",
            "ouder@example.com",
            coteDorQuantity: 2,
            lotusQuantity: 1);

        Assert.Equal("ouder@example.com", options.CustomerEmail);
        Assert.Equal(2, options.LineItems.Count);
        Assert.Contains(options.LineItems, item =>
            item.Price == "price_cookie_cotedor" && item.Quantity == 2);
        Assert.Contains(options.LineItems, item =>
            item.Price == "price_cookie_lotus" && item.Quantity == 1);
        Assert.Equal("cookie-sale-2026", options.Metadata["flow"]);
        Assert.Equal("order-123", options.Metadata["orderId"]);
        Assert.Equal(2, options.Metadata.Count);
        Assert.Contains("/koekjesverkoop/betaling/success", options.SuccessUrl);
        Assert.EndsWith("/koekjesverkoop/betaling/cancel", options.CancelUrl);
    }
}
