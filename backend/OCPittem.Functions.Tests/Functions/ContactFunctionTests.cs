using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Functions;

public class ContactFunctionTests
{
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IConfiguration _config = Substitute.For<IConfiguration>();
    private readonly ILogger<ContactFunction> _logger = Substitute.For<ILogger<ContactFunction>>();
    private readonly ContactFunction _sut;

    public ContactFunctionTests()
    {
        _config["App:ContactEmail"].Returns("committee@example.com");
        _sut = new ContactFunction(_email, _config, _logger);
    }

    [Fact]
    public async Task Run_ValidRequest_SendsEmailAndReturnsSuccess()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            name = "Test User",
            email = "test@example.com",
            subject = "Vraag",
            message = "Dit is een test bericht."
        });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        await _email.Received(1).SendContactNotificationAsync(
            "Test User", "test@example.com", "Vraag", "Dit is een test bericht.", "committee@example.com");
    }

    [Fact]
    public async Task Run_MissingFields_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            name = "Test",
            email = "",
            subject = "Subject",
            message = "Body"
        });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_FieldTooLong_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            name = new string('A', 201),
            email = "test@example.com",
            subject = "Subject",
            message = "Body"
        });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_InvalidJson_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateJsonRequest("not valid json {{{");

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_EmailServiceThrows_Returns500()
    {
        var req = HttpRequestHelper.CreateJsonRequest(new
        {
            name = "Test",
            email = "test@example.com",
            subject = "Subject",
            message = "Body"
        });
        _email.SendContactNotificationAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new Exception("SMTP error"));

        var result = await _sut.Run(req);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }
}
