using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();
    private readonly ILogger<SponsorRequestFunction> _logger = Substitute.For<ILogger<SponsorRequestFunction>>();
    private readonly SponsorRequestFunction _sut;

    public SponsorRequestFunctionTests()
    {
        _config["App:ContactEmail"].Returns("committee@example.com");
        _sut = new SponsorRequestFunction(_storage, _email, _config, _logger);
    }

    [Fact]
    public async Task Run_ValidRequest_SavesAndSendsEmails()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName = "TestBV",
            contactName = "Jan",
            email = "jan@testbv.be",
            phone = "0471234567",
            package = "Gold",
            message = "Interesse in sponsoring"
        });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        await _storage.Received(1).SaveSponsorRequestAsync(
            Arg.Is<SponsorRequestEntity>(e =>
                e.CompanyName == "TestBV" &&
                e.Email == "jan@testbv.be" &&
                e.Package == "Gold"));
        await _email.Received(1).SendSponsorConfirmationAsync("jan@testbv.be", "TestBV", "Gold");
        await _email.Received(1).SendContactNotificationAsync(
            "Jan", "jan@testbv.be", Arg.Any<string>(), Arg.Any<string>(), "committee@example.com");
    }

    [Fact]
    public async Task Run_MissingRequiredFields_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName = "",
            contactName = "Jan",
            email = "jan@testbv.be",
            phone = "",
            package = "Gold",
            message = ""
        });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_InvalidJson_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest("not valid json");

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_StorageThrows_Returns500()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            companyName = "TestBV",
            contactName = "Jan",
            email = "jan@testbv.be",
            phone = "",
            package = "Gold",
            message = ""
        });
        _storage.SaveSponsorRequestAsync(Arg.Any<SponsorRequestEntity>())
            .ThrowsAsync(new Exception("Storage error"));

        var result = await _sut.Run(req);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }
}
