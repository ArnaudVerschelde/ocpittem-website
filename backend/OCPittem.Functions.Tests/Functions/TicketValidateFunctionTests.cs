using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Functions;

public class TicketValidateFunctionTests
{
    private readonly ILogger<TicketValidateFunction> _logger = Substitute.For<ILogger<TicketValidateFunction>>();
    private readonly TicketValidateFunction _sut;

    public TicketValidateFunctionTests()
    {
        _sut = new TicketValidateFunction(_logger);
    }

    [Fact]
    public void Run_NoCode_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateGetRequest();

        var result = _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Run_EmptyCode_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", "" } });

        var result = _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Run_InvalidFormat_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", "no-colon-here" } });

        var result = _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Run_InvalidSignature_ReturnsInvalid()
    {
        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string>
        {
            { "code", "some-ticket-id:wrongsignature!" }
        });

        var result = _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Contains("false", ok.Value.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_ValidCode_ReturnsValid()
    {
        var ticketId = Guid.NewGuid().ToString();
        var signature = ComputeExpectedSignature(ticketId);
        var code = $"{ticketId}:{signature}";

        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", code } });

        var result = _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Contains("true", ok.Value.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeExpectedSignature(string ticketId)
    {
        var secret = Encoding.UTF8.GetBytes("ocpittem-ticket-secret-change-me");
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticketId));
        return Convert.ToBase64String(hash)[..16];
    }
}
