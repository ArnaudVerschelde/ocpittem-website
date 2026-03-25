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
}
