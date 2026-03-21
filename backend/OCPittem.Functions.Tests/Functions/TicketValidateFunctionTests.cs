using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Functions;

public class TicketValidateFunctionTests
{
    private const string TestSecret = "test-hmac-secret";

    private readonly ILogger<TicketValidateFunction> _logger = Substitute.For<ILogger<TicketValidateFunction>>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly TicketValidateFunction _sut;

    public TicketValidateFunctionTests()
    {
        var options = Options.Create(new AppOptions { TicketHmacSecret = TestSecret });
        _sut = new TicketValidateFunction(options, _storage, _logger);
    }

    [Fact]
    public async Task Run_NoCode_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateGetRequest();

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_EmptyCode_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", "" } });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_InvalidFormat_ReturnsBadRequest()
    {
        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", "no-colon-here" } });

        var result = await _sut.Run(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Run_InvalidSignature_ReturnsInvalid()
    {
        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string>
        {
            { "code", "some-ticket-id:wrongsignature!" }
        });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Contains("false", ok.Value.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Run_TicketNotFound_ReturnsInvalid()
    {
        var ticketId = Guid.NewGuid().ToString();
        var signature = ComputeExpectedSignature(ticketId);
        var code = $"{ticketId}:{signature}";

        _storage.GetTicketByIdAsync(ticketId).Returns((TicketEntity?)null);

        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", code } });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Contains("false", ok.Value.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Run_AlreadyScanned_ReturnsInvalid()
    {
        var ticketId = Guid.NewGuid().ToString();
        var signature = ComputeExpectedSignature(ticketId);
        var code = $"{ticketId}:{signature}";

        _storage.GetTicketByIdAsync(ticketId).Returns(new TicketEntity
        {
            PartitionKey = "order-1",
            RowKey = ticketId,
            TicketType = nameof(TicketKind.Toegang),
            ScannedAt = DateTime.UtcNow.AddMinutes(-5)
        });

        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", code } });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Contains("false", ok.Value.ToString()!, StringComparison.OrdinalIgnoreCase);
        await _storage.DidNotReceive().MarkTicketScannedAsync(Arg.Any<TicketEntity>());
    }

    [Fact]
    public async Task Run_ValidCode_ReturnsValid()
    {
        var ticketId = Guid.NewGuid().ToString();
        var signature = ComputeExpectedSignature(ticketId);
        var code = $"{ticketId}:{signature}";

        _storage.GetTicketByIdAsync(ticketId).Returns(new TicketEntity
        {
            PartitionKey = "order-1",
            RowKey = ticketId,
            TicketType = nameof(TicketKind.Toegang),
            ScannedAt = null
        });

        var req = HttpRequestHelper.CreateGetRequest(new Dictionary<string, string> { { "code", code } });

        var result = await _sut.Run(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Contains("true", ok.Value.ToString()!, StringComparison.OrdinalIgnoreCase);
        await _storage.Received(1).MarkTicketScannedAsync(Arg.Any<TicketEntity>());
    }

    private static string ComputeExpectedSignature(string ticketId)
    {
        var secret = Encoding.UTF8.GetBytes(TestSecret);
        using var hmac = new HMACSHA256(secret);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticketId));
        return Convert.ToBase64String(hash)[..16];
    }
}
