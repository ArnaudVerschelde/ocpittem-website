using Microsoft.Extensions.Logging;
using NSubstitute;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class MailjetEmailServiceTests
{
    private readonly ILogger<MailjetEmailService> _logger = Substitute.For<ILogger<MailjetEmailService>>();

    private MailjetEmailService CreateDisabledSut() =>
        new(new MailjetOptions { FromEmail = "from@example.com", FromName = "Test" }, enabled: false, _logger);

    [Fact]
    public void Constructor_EnabledWithoutApiKeys_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new MailjetEmailService(
                new MailjetOptions { FromEmail = "from@example.com", FromName = "Test" },
                enabled: true,
                _logger));
    }

    [Fact]
    public void Constructor_EnabledWithApiKeys_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            new MailjetEmailService(
                new MailjetOptions
                {
                    ApiKey = "key",
                    ApiSecret = "secret",
                    FromEmail = "from@example.com",
                    FromName = "Test"
                },
                enabled: true,
                _logger));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendTicketConfirmationAsync_EmailDisabled_CompletesWithoutThrowing()
    {
        var sut = CreateDisabledSut();
        var tickets = new List<TicketPdfData>
        {
            new("ticket-1", "ticket-1:abc123", nameof(TicketKind.Toegang), false)
        };

        await sut.SendTicketConfirmationAsync("to@example.com", "Naam", 1, 0, 0, 0, 0, tickets, null);
    }

    [Fact]
    public async Task SendContactNotificationAsync_EmailDisabled_CompletesWithoutThrowing()
    {
        var sut = CreateDisabledSut();

        await sut.SendContactNotificationAsync("Jan", "jan@example.com", "Vraag", "Bericht", "contact@example.com");
    }

    [Fact]
    public async Task SendSponsorConfirmationAsync_EmailDisabled_CompletesWithoutThrowing()
    {
        var sut = CreateDisabledSut();

        await sut.SendSponsorConfirmationAsync("bedrijf@example.com", "Bedrijf NV", "Goud");
    }

    [Fact]
    public async Task SendDailyReportAsync_EmailDisabled_CompletesWithoutThrowing()
    {
        var sut = CreateDisabledSut();
        var stats = new DailyReportStats(5, 3, 6, 2, 1, 1, 0, 98m, 2, 1, 0, 1, 0, 0, 0, 0, 250m);

        await sut.SendDailyReportAsync(["r1@example.com", "r2@example.com"], [1, 2, 3], stats, DateTime.UtcNow);
    }
}
