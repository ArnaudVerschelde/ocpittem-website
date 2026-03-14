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

public class TicketOrderFunctionTests
{
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<TicketOrderFunction> _logger = Substitute.For<ILogger<TicketOrderFunction>>();
    private readonly TicketOrderFunction _sut;

    public TicketOrderFunctionTests()
    {
        _sut = new TicketOrderFunction(_stripe, _storage, _logger);
    }

    [Fact]
    public async Task CreateCheckout_ValidRequest_ReturnsCheckoutUrl()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new { name = "Jan Janssen", email = "jan@example.com", quantity = 2 });
        _stripe.CreateCheckoutSessionAsync(Arg.Any<string>(), "jan@example.com", "Jan Janssen", 2)
            .Returns(new StripeCheckoutResult("https://checkout.stripe.com/session123", "sess_123"));

        var result = await _sut.CreateCheckout(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        await _storage.Received(1).SaveOrderAsync(Arg.Is<OrderEntity>(o =>
            o.Email == "jan@example.com" &&
            o.Name == "Jan Janssen" &&
            o.Quantity == 2 &&
            o.Status == "pending"));
    }

    [Fact]
    public async Task CreateCheckout_InvalidJson_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest("not valid json {{{");

        var result = await _sut.CreateCheckout(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_MissingName_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new { name = "", email = "test@example.com", quantity = 1 });

        var result = await _sut.CreateCheckout(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_MissingEmail_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new { name = "Test", email = "", quantity = 1 });

        var result = await _sut.CreateCheckout(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task CreateCheckout_InvalidQuantity_ReturnsBadRequest(int quantity)
    {
        var req = HttpRequestHelper.CreateJsonRequest(new { name = "Test", email = "test@example.com", quantity });

        var result = await _sut.CreateCheckout(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateCheckout_StripeThrows_Returns500()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new { name = "Test", email = "test@example.com", quantity = 1 });
        _stripe.CreateCheckoutSessionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .ThrowsAsync(new Exception("Stripe unavailable"));

        var result = await _sut.CreateCheckout(req);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }
}
