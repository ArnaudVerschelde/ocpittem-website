using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class DailyReportServiceTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<DailyReportService> _logger = Substitute.For<ILogger<DailyReportService>>();

    private DailyReportService CreateSut(string recipients) =>
        new(_storage, _email, Options.Create(new AppOptions { ReportRecipients = recipients }), _logger);

    [Fact]
    public async Task SendDailyReportAsync_NoRecipients_SkipsEmail()
    {
        var sut = CreateSut("");

        await sut.SendDailyReportAsync();

        await _email.DidNotReceive().SendDailyReportAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<byte[]>(),
            Arg.Any<DailyReportStats>(),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SendDailyReportAsync_WithRecipients_SendsEmail()
    {
        _storage.GetAllOrdersAsync().Returns(new List<OrderEntity>());
        _storage.GetAllSponsorRequestsAsync().Returns(new List<SponsorRequestEntity>());

        var sut = CreateSut("a@example.com,b@example.com");

        await sut.SendDailyReportAsync();

        await _email.Received(1).SendDailyReportAsync(
            Arg.Is<IReadOnlyList<string>>(r => r.Count == 2 && r[0] == "a@example.com" && r[1] == "b@example.com"),
            Arg.Any<byte[]>(),
            Arg.Any<DailyReportStats>(),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SendDailyReportAsync_PaidOrders_CalculatesStatsCorrectly()
    {
        var orders = new List<OrderEntity>
        {
            new() { Status = nameof(OrderStatus.Paid), ToegangsticketCount = 2, EtenPartyCount = 1, VegetarischCount = 1, Drankkaart10Count = 0, Drankkaart20Count = 1 },
            new() { Status = nameof(OrderStatus.Pending), ToegangsticketCount = 3 },
            new() { Status = nameof(OrderStatus.Failed), ToegangsticketCount = 1 },
        };
        var sponsors = new List<SponsorRequestEntity> { new(), new() };

        _storage.GetAllOrdersAsync().Returns(orders);
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);

        var sut = CreateSut("test@example.com");
        await sut.SendDailyReportAsync();

        // TotalRevenue = 2*8 + 1*50 + 0*10 + 1*20 = 86
        await _email.Received(1).SendDailyReportAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<byte[]>(),
            Arg.Is<DailyReportStats>(s =>
                s.TotalOrders == 3 &&
                s.PaidOrders == 1 &&
                s.TotalToegangstickets == 2 &&
                s.TotalEtenPartyTickets == 1 &&
                s.TotalVegetarisch == 1 &&
                s.TotalDrankkaart10 == 0 &&
                s.TotalDrankkaart20 == 1 &&
                s.TotalRevenue == 86m &&
                s.TotalSponsorRequests == 2),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SendDailyReportAsync_NoPaidOrders_SendsZeroStats()
    {
        var orders = new List<OrderEntity>
        {
            new() { Status = nameof(OrderStatus.Pending), ToegangsticketCount = 2 },
            new() { Status = nameof(OrderStatus.Failed), ToegangsticketCount = 1 },
        };

        _storage.GetAllOrdersAsync().Returns(orders);
        _storage.GetAllSponsorRequestsAsync().Returns(new List<SponsorRequestEntity>());

        var sut = CreateSut("test@example.com");
        await sut.SendDailyReportAsync();

        await _email.Received(1).SendDailyReportAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<byte[]>(),
            Arg.Is<DailyReportStats>(s =>
                s.TotalOrders == 2 &&
                s.PaidOrders == 0 &&
                s.TotalToegangstickets == 0 &&
                s.TotalRevenue == 0m),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SendDailyReportAsync_AttachesNonEmptyExcel()
    {
        _storage.GetAllOrdersAsync().Returns(new List<OrderEntity>());
        _storage.GetAllSponsorRequestsAsync().Returns(new List<SponsorRequestEntity>());

        var sut = CreateSut("test@example.com");
        await sut.SendDailyReportAsync();

        await _email.Received(1).SendDailyReportAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Is<byte[]>(b => b.Length > 0),
            Arg.Any<DailyReportStats>(),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SendDailyReportAsync_PaidSponsors_CalculatesSponsorStatsCorrectly()
    {
        var sponsors = new List<SponsorRequestEntity>
        {
            new() { Status = "Paid", Package = "goud", ExtraEtenPartyCount = 2, ExtraVegetarischCount = 1, ExtraDrankkaart20Count = 1 },
            new() { Status = "Paid", Package = "brons", ExtraEtenPartyCount = 0, ExtraVegetarischCount = 0, ExtraDrankkaart20Count = 0 },
            new() { Status = "Pending", Package = "zilver" },
        };
        _storage.GetAllOrdersAsync().Returns(new List<OrderEntity>());
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);

        var sut = CreateSut("test@example.com");
        await sut.SendDailyReportAsync();

        // Revenue: goud(500) + 2*50 + 1*20 = 620 | brons(100) = 100 | total = 720
        await _email.Received(1).SendDailyReportAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<byte[]>(),
            Arg.Is<DailyReportStats>(s =>
                s.TotalSponsorRequests == 3 &&
                s.PaidSponsorOrders == 2 &&
                s.TotalSponsorBrons == 1 &&
                s.TotalSponsorZilver == 0 &&
                s.TotalSponsorGoud == 1 &&
                s.TotalSponsorExtraEtenParty == 2 &&
                s.TotalSponsorExtraVegetarisch == 1 &&
                s.TotalSponsorExtraDrankkaart20 == 1 &&
                s.TotalSponsorRevenue == 720m),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SendDailyReportAsync_SponsorWithCustomAttestationTotal_UsesCustomTotalForRevenue()
    {
        var sponsors = new List<SponsorRequestEntity>
        {
            // goud normaal = 500, maar betaalde 1000 (dubbel pakket)
            new() { Status = "Paid", Package = "goud", ExtraEtenPartyCount = 4, ExtraDrankkaart20Count = 0, CustomAttestationTotal = 1000m },
            // brons normaal = 100, geen override
            new() { Status = "Paid", Package = "brons", ExtraEtenPartyCount = 0, ExtraDrankkaart20Count = 0 },
        };
        _storage.GetAllOrdersAsync().Returns(new List<OrderEntity>());
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);

        var sut = CreateSut("test@example.com");
        await sut.SendDailyReportAsync();

        // Revenue: goud custom(1000) + brons(100) = 1100
        await _email.Received(1).SendDailyReportAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<byte[]>(),
            Arg.Is<DailyReportStats>(s => s.TotalSponsorRevenue == 1100m),
            Arg.Any<DateTime>());
    }
}