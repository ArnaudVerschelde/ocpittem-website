using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Functions;

public class CookieSaleOrderFunctionTests
{
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<CookieSaleOrderFunction> _logger =
        Substitute.For<ILogger<CookieSaleOrderFunction>>();

    [Fact]
    public void GetConfig_EmptyCatalog_DisablesOrdering()
    {
        var sut = CreateSut([]);
        var request = HttpRequestHelper.CreateJsonRequest(new { });

        var result = sut.GetConfig(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var config = Assert.IsType<CookieSalePublicConfigResponse>(ok.Value);
        Assert.False(config.Enabled);
        Assert.Empty(config.Classes);
    }

    [Fact]
    public async Task CreateCheckout_EmptyCatalog_ReturnsServiceUnavailable()
    {
        var sut = CreateSut([]);
        var request = CreateValidRequest();

        var result = await sut.CreateCheckout(request);

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        await _storage.DidNotReceive().SaveCookieOrderAsync(Arg.Any<CookieOrderEntity>());
    }

    [Fact]
    public async Task CreateCheckout_ValidRequest_PersistsPendingBeforeStripe()
    {
        var sut = CreateSut(["Testklas"]);
        var request = CreateValidRequest();
        _stripe.CreateCookieSaleCheckoutSessionAsync(
                Arg.Any<string>(),
                "ouder@example.com",
                2,
                1)
            .Returns(new StripeCheckoutResult("https://checkout.stripe.test/cookie", "cs_test_cookie"));

        var result = await sut.CreateCheckout(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CreateCheckoutResponse>(ok.Value);
        Assert.Equal("https://checkout.stripe.test/cookie", response.CheckoutUrl);

        Received.InOrder(() =>
        {
            _storage.SaveCookieOrderAsync(Arg.Is<CookieOrderEntity>(order =>
                order.PaymentStatus == nameof(CookieOrderStatus.Pending)
                && order.TotalAmountCents == 3100
                && order.TotalPackages == 3
                && order.ClassName == "Testklas"
                && order.StudentName == "Test Leerling"
                && order.ConfirmationNumber.StartsWith("KV26-")));
            _stripe.CreateCookieSaleCheckoutSessionAsync(
                Arg.Any<string>(),
                "ouder@example.com",
                2,
                1);
        });

        await _storage.Received(1)
            .SetCookieOrderStripeSessionAsync(Arg.Any<string>(), "cs_test_cookie");
    }

    [Fact]
    public async Task CreateCheckout_InvalidClass_ReturnsBadRequest()
    {
        var sut = CreateSut(["Andere klas"]);
        var request = CreateValidRequest();

        var result = await sut.CreateCheckout(request);

        Assert.IsType<BadRequestObjectResult>(result);
        await _stripe.DidNotReceive().CreateCookieSaleCheckoutSessionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CreateCheckout_StripeFailure_MarksPendingOrderFailed()
    {
        var sut = CreateSut(["Testklas"]);
        var request = CreateValidRequest();
        _stripe.CreateCookieSaleCheckoutSessionAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>())
            .ThrowsAsync(new InvalidOperationException("Stripe unavailable"));

        var result = await sut.CreateCheckout(request);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.StatusCode);
        await _storage.Received(1)
            .TryMarkCookieOrderStatusAsync(Arg.Any<string>(), CookieOrderStatus.Failed);
    }

    [Fact]
    public async Task GetOrderStatus_PaidOrder_ReturnsConfirmationNumberOnly()
    {
        var sut = CreateSut(["Testklas"]);
        var request = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string>
        {
            ["session_id"] = "cs_test_paid",
        });
        _storage.GetCookieOrderByStripeSessionAsync("cs_test_paid").Returns(new CookieOrderEntity
        {
            PaymentStatus = nameof(CookieOrderStatus.Paid),
            ConfirmationNumber = "KV26-23456789ABCD",
            Name = "Niet terugsturen",
            Email = "niet@example.com",
        });

        var result = await sut.GetOrderStatus(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CookieSaleOrderStatusResponse>(ok.Value);
        Assert.Equal(nameof(CookieOrderStatus.Paid), response.PaymentStatus);
        Assert.Equal("KV26-23456789ABCD", response.ConfirmationNumber);
    }

    private CookieSaleOrderFunction CreateSut(IReadOnlyList<string> classes) =>
        new(_stripe, _storage, _logger, classes);

    private static HttpRequest CreateValidRequest() =>
        HttpRequestHelper.CreateJsonRequest(new
        {
            name = "Test Ouder",
            studentName = "  Test Leerling  ",
            email = "ouder@example.com",
            className = "Testklas",
            coteDorQuantity = 2,
            lotusQuantity = 1,
        });
}
