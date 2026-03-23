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

public class SponsorRequestFunctionTests
{
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<SponsorRequestFunction> _logger = Substitute.For<ILogger<SponsorRequestFunction>>();
    private readonly SponsorRequestFunction _sut;

    // A real KBC enterprise number whose check digit is correct.
    private const string ValidEnterpriseNumber = "0403.227.515";

    public SponsorRequestFunctionTests()
    {
        _stripe.CreateSponsorCheckoutSessionAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new StripeCheckoutResult("https://checkout.stripe.com/test", "cs_test_123"));

        _sut = new SponsorRequestFunction(_stripe, _storage, _logger);
    }

    // ------------------------------------------------------------------
    // Happy path
    // ------------------------------------------------------------------

    [Fact]
    public async Task Run_ValidRequest_CreatesCheckoutAndSavesEntity()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName     = "TestBV",
            contactName     = "Jan Janssen",
            email           = "jan@testbv.be",
            phone           = "0471234567",
            package         = "zilver",
            message         = "Interesse in sponsoring",
            enterpriseNumber = ValidEnterpriseNumber,
        });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        await _stripe.Received(1).CreateSponsorCheckoutSessionAsync(
            Arg.Any<string>(), "jan@testbv.be", "TestBV", "zilver", 0, 0);

        await _storage.Received(1).SaveSponsorRequestAsync(
            Arg.Is<SponsorRequestEntity>(e =>
                e.CompanyName      == "TestBV" &&
                e.Email            == "jan@testbv.be" &&
                e.Package          == "zilver" &&
                e.EnterpriseNumber == "0403.227.515" &&
                e.Status           == "Pending"));
    }

    [Fact]
    public async Task Run_ValidRequest_ReturnsCheckoutUrl()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName      = "TestBV",
            contactName      = "Jan",
            email            = "jan@testbv.be",
            phone            = "",
            package          = "goud",
            message          = "",
            enterpriseNumber = ValidEnterpriseNumber,
        });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SponsorCheckoutResponse>(ok.Value);
        Assert.Equal("https://checkout.stripe.com/test", response.CheckoutUrl);
    }

    // ------------------------------------------------------------------
    // Validation — required fields
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("", "Jan", "jan@test.be", "zilver", ValidEnterpriseNumber)]   // missing companyName
    [InlineData("BV", "", "jan@test.be", "zilver", ValidEnterpriseNumber)]    // missing contactName
    [InlineData("BV", "Jan", "", "zilver", ValidEnterpriseNumber)]            // missing email
    [InlineData("BV", "Jan", "jan@test.be", "", ValidEnterpriseNumber)]       // missing package
    [InlineData("BV", "Jan", "jan@test.be", "zilver", "")]                    // missing enterpriseNumber
    public async Task Run_MissingRequiredField_ReturnsBadRequest(
        string company, string contact, string email, string package, string enterprise)
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName      = company,
            contactName      = contact,
            email,
            phone            = "",
            package,
            message          = "",
            enterpriseNumber = enterprise,
        });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ------------------------------------------------------------------
    // Validation — enterprise number
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("0403.227.999")]   // wrong check digit
    [InlineData("1234567890")]     // invalid (starts with 1 but wrong check)
    [InlineData("ABCDEFGHIJ")]     // non-numeric
    [InlineData("040322751")]      // too short
    public async Task Run_InvalidEnterpriseNumber_ReturnsBadRequest(string enterprise)
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName      = "TestBV",
            contactName      = "Jan",
            email            = "jan@testbv.be",
            phone            = "",
            package          = "zilver",
            message          = "",
            enterpriseNumber = enterprise,
        });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ------------------------------------------------------------------
    // Validation — package name
    // ------------------------------------------------------------------

    [Fact]
    public async Task Run_InvalidPackage_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName      = "TestBV",
            contactName      = "Jan",
            email            = "jan@testbv.be",
            phone            = "",
            package          = "platinum",
            message          = "",
            enterpriseNumber = ValidEnterpriseNumber,
        });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ------------------------------------------------------------------
    // Error handling
    // ------------------------------------------------------------------

    [Fact]
    public async Task Run_InvalidJson_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest("not valid json");

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_StripeThrows_Returns500()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName      = "TestBV",
            contactName      = "Jan",
            email            = "jan@testbv.be",
            phone            = "",
            package          = "zilver",
            message          = "",
            enterpriseNumber = ValidEnterpriseNumber,
        });

        _stripe.CreateSponsorCheckoutSessionAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .ThrowsAsync(new Exception("Stripe unavailable"));

        var result = await _sut.Run(req);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }
}
