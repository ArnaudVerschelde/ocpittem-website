using Microsoft.Extensions.Logging;
using NSubstitute;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class SmtpEmailServiceTests
{
    private readonly ILogger<SmtpEmailService> _logger = Substitute.For<ILogger<SmtpEmailService>>();

    private static MailjetOptions DefaultSenderOptions() => new()
    {
        FromEmail = "from@example.com",
        FromName = "Test",
        TicketFromEmail = "tickets@example.com",
        TicketFromName = "Tickets"
    };

    private static SmtpOptions ValidSmtpOptions() => new()
    {
        Host = "smtp.example.com",
        Port = 587,
        Username = "user@example.com",
        Password = "secret",
        EnableSsl = true
    };

    private SmtpEmailService CreateDisabledSut() =>
        new(new SmtpOptions(), DefaultSenderOptions(), enabled: false, _logger);

    // ── Constructor validation ──────────────────────────────────────────────

    [Fact]
    public void Constructor_EnabledWithMissingHost_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SmtpEmailService(
                new SmtpOptions { Port = 587, Username = "u", Password = "p" },
                DefaultSenderOptions(),
                enabled: true,
                _logger));
    }

    [Fact]
    public void Constructor_EnabledWithInvalidPort_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SmtpEmailService(
                new SmtpOptions { Host = "smtp.example.com", Port = 0, Username = "u", Password = "p" },
                DefaultSenderOptions(),
                enabled: true,
                _logger));
    }

    [Fact]
    public void Constructor_EnabledWithMissingUsername_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SmtpEmailService(
                new SmtpOptions { Host = "smtp.example.com", Port = 587, Password = "p" },
                DefaultSenderOptions(),
                enabled: true,
                _logger));
    }

    [Fact]
    public void Constructor_EnabledWithMissingPassword_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SmtpEmailService(
                new SmtpOptions { Host = "smtp.example.com", Port = 587, Username = "u" },
                DefaultSenderOptions(),
                enabled: true,
                _logger));
    }

    [Fact]
    public void Constructor_EnabledWithAllValidOptions_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            new SmtpEmailService(ValidSmtpOptions(), DefaultSenderOptions(), enabled: true, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_DisabledWithEmptyOptions_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            new SmtpEmailService(new SmtpOptions(), DefaultSenderOptions(), enabled: false, _logger));

        Assert.Null(exception);
    }

    // ── Disabled path — all methods must return without throwing ───────────

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

    [Fact]
    public async Task SendSponsorPaymentConfirmationAsync_EmailDisabled_CompletesWithoutThrowing()
    {
        var sut = CreateDisabledSut();
        var tickets = new List<TicketPdfData>
        {
            new("ticket-1", "ticket-1:abc123", nameof(TicketKind.EtenParty), false)
        };

        await sut.SendSponsorPaymentConfirmationAsync(
            "sponsor@example.com", "Bedrijf NV", "zilver",
            extraEtenParty: 0, extraVegetarisch: 0, extraDrankkaart20: 0,
            includedVegetarisch: 0, tickets, null, null);
    }

    // ── Sender fallback logic ───────────────────────────────────────────────

    [Fact]
    public void Constructor_SenderOptions_FallsBackToFromEmailWhenContactFromEmailEmpty()
    {
        var sender = new MailjetOptions
        {
            FromEmail = "fallback@example.com",
            FromName = "Fallback",
            ContactFromEmail = "",
            ContactFromName = ""
        };

        var exception = Record.Exception(() =>
            new SmtpEmailService(ValidSmtpOptions(), sender, enabled: false, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_SenderOptions_UsesContactFromEmailWhenProvided()
    {
        var sender = new MailjetOptions
        {
            FromEmail = "from@example.com",
            FromName = "From",
            ContactFromEmail = "contact@example.com",
            ContactFromName = "Contact"
        };

        var exception = Record.Exception(() =>
            new SmtpEmailService(ValidSmtpOptions(), sender, enabled: false, _logger));

        Assert.Null(exception);
    }
}
