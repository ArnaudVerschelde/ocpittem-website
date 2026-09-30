using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class CookieSaleReportServiceTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<CookieSaleReportService> _logger =
        Substitute.For<ILogger<CookieSaleReportService>>();

    [Fact]
    public async Task SendDailyReportAsync_UsesPaidOrdersAndCorrectTotals()
    {
        _storage.GetPaidCookieOrdersAsync().Returns(
        [
            CreateOrder("KV26-AAAAAAAAAAAA", "Klas A", "Anna", 2, 1, CookieOrderStatus.Paid),
            CreateOrder(
                "KV26-BBBBBBBBBBBB",
                "",
                "Bert",
                0,
                3,
                CookieOrderStatus.Paid,
                orderType: "staff",
                staffCategory: "Leerkracht"),
            CreateOrder("KV26-CCCCCCCCCCCC", "Klas C", "Chris", 9, 9, CookieOrderStatus.Pending),
        ]);
        var sut = CreateSut("report@example.com");

        await sut.SendDailyReportAsync();

        await _email.Received(1).SendCookieSaleDailyReportAsync(
            Arg.Is<IReadOnlyList<string>>(recipients => recipients.SequenceEqual(new[] { "report@example.com" })),
            Arg.Is<byte[]>(bytes => bytes.Length > 0),
            Arg.Is<CookieSaleReportStats>(stats =>
                stats.TotalOrders == 2
                && stats.TotalCoteDorPackages == 2
                && stats.TotalLotusPackages == 4
                && stats.TotalPackages == 6
                && stats.TotalAmountCents == 5800),
            Arg.Any<DateTime>());
    }

    [Fact]
    public void BuildExcel_ContainsPaidOrdersConfirmationAndClassSheets()
    {
        var orders = new List<CookieOrderEntity>
        {
            CreateOrder(
                "KV26-BBBBBBBBBBBB",
                "",
                "Bert",
                0,
                3,
                CookieOrderStatus.Paid,
                orderType: "staff",
                staffCategory: "Leerkracht"),
            CreateOrder("KV26-AAAAAAAAAAAA", "Klas A", "Anna", 2, 1, CookieOrderStatus.Paid),
            CreateOrder("KV26-PENDING00000", "Klas A", "Pending", 5, 5, CookieOrderStatus.Pending),
        };

        var bytes = CookieSaleReportService.BuildExcel(orders);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var overview = workbook.Worksheet("Overzicht");
        Assert.Equal("KV26-AAAAAAAAAAAA", overview.Cell(2, 1).GetString());
        Assert.Equal("KV26-BBBBBBBBBBBB", overview.Cell(3, 1).GetString());
        Assert.Equal("Naam leerling", overview.Cell(1, 2).GetString());
        Assert.Equal("Leerling Anna", overview.Cell(2, 2).GetString());
        Assert.DoesNotContain(
            overview.CellsUsed(),
            cell => cell.GetString() == "KV26-PENDING00000");

        var classA = workbook.Worksheet("Klas A");
        Assert.Equal("KV26-AAAAAAAAAAAA", classA.Cell(2, 1).GetString());
        Assert.Equal("Naam leerling", classA.Cell(1, 2).GetString());
        Assert.Equal("Leerling Anna", classA.Cell(2, 2).GetString());
        Assert.DoesNotContain(classA.CellsUsed(), cell => cell.GetString() == "KV26-BBBBBBBBBBBB");

        Assert.False(workbook.Worksheets.TryGetWorksheet("Klas B", out _));

        var staff = workbook.Worksheet("Personeel");
        Assert.Equal("KV26-BBBBBBBBBBBB", staff.Cell(2, 1).GetString());
        Assert.Equal("Leerkracht", staff.Cell(2, 3).GetString());
        Assert.DoesNotContain(staff.CellsUsed(), cell => cell.GetString() == "KV26-AAAAAAAAAAAA");
        Assert.DoesNotContain(staff.CellsUsed(), cell => cell.GetString() == "KV26-PENDING00000");

        AssertSummaryValue(overview, "Bestellingen", 2m);
        AssertSummaryValue(overview, "Côte d'Or", 2m);
        AssertSummaryValue(overview, "Lotus", 4m);
        AssertSummaryValue(overview, "Totaal pakketten", 6m);
        AssertSummaryValue(overview, "Totale omzet", 58m);
    }

    [Fact]
    public void BuildExcel_SeparatesStaffAndIncludesThemInGlobalTotals()
    {
        var legacyStudentOrder = CreateOrder(
            "KV26-LEGACY000001",
            "Klas A",
            "Anna",
            1,
            0,
            CookieOrderStatus.Paid,
            orderType: "");
        var staffOrder = CreateOrder(
            "KV26-STAFF0000001",
            "",
            "Sara",
            0,
            2,
            CookieOrderStatus.Paid,
            orderType: "staff",
            staffCategory: "Zorg");

        var bytes = CookieSaleReportService.BuildExcel([legacyStudentOrder, staffOrder]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var overview = workbook.Worksheet("Overzicht");
        Assert.Contains(overview.CellsUsed(), cell => cell.GetString() == "KV26-LEGACY000001");
        Assert.Contains(overview.CellsUsed(), cell => cell.GetString() == "KV26-STAFF0000001");
        AssertSummaryValue(overview, "Bestellingen", 2m);
        AssertSummaryValue(overview, "Totaal pakketten", 3m);
        AssertSummaryValue(overview, "Totale omzet", 29m);

        var classSheet = workbook.Worksheet("Klas A");
        Assert.Contains(classSheet.CellsUsed(), cell => cell.GetString() == "KV26-LEGACY000001");
        Assert.DoesNotContain(classSheet.CellsUsed(), cell => cell.GetString() == "KV26-STAFF0000001");

        var staffSheet = workbook.Worksheet("Personeel");
        Assert.Equal("Personeelsgroep", staffSheet.Cell(1, 3).GetString());
        Assert.Contains(staffSheet.CellsUsed(), cell => cell.GetString() == "KV26-STAFF0000001");
        Assert.Contains(staffSheet.CellsUsed(), cell => cell.GetString() == "SAMENVATTING PER PERSONEELSGROEP");
        Assert.Contains(staffSheet.CellsUsed(), cell => cell.GetString() == "Zorg");
        Assert.DoesNotContain(staffSheet.CellsUsed(), cell => cell.GetString() == "KV26-LEGACY000001");
    }

    [Fact]
    public void BuildExcel_SeparatesSupportersAndIncludesThemInGlobalTotals()
    {
        var studentOrder = CreateOrder("KV26-STUDENT00001", "Klas A", "Anna", 1, 0, CookieOrderStatus.Paid);
        var legacyStudentOrder = CreateOrder(
            "KV26-LEGACY000001",
            "Klas A",
            "Bram",
            0,
            1,
            CookieOrderStatus.Paid,
            orderType: "");
        var staffOrder = CreateOrder(
            "KV26-STAFF0000001",
            "",
            "Sara",
            0,
            2,
            CookieOrderStatus.Paid,
            orderType: "staff",
            staffCategory: "Zorg");
        var supporterOrder = CreateOrder(
            "KV26-SUPPORTER001",
            "",
            "Sven",
            2,
            1,
            CookieOrderStatus.Paid,
            orderType: "supporter");
        var pendingSupporterOrder = CreateOrder(
            "KV26-SUPPENDING01",
            "",
            "Pending",
            5,
            5,
            CookieOrderStatus.Pending,
            orderType: "supporter");

        var bytes = CookieSaleReportService.BuildExcel(
            [supporterOrder, staffOrder, studentOrder, legacyStudentOrder, pendingSupporterOrder]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(
            ["Overzicht", "Klas A", "Personeel", "Sympathisanten"],
            workbook.Worksheets.Select(worksheet => worksheet.Name).ToArray());

        var overview = workbook.Worksheet("Overzicht");
        Assert.Equal("KV26-SUPPORTER001", overview.Cell(5, 1).GetString());
        Assert.Equal("Sympathisant", overview.Cell(5, 11).GetString());
        Assert.Equal(string.Empty, overview.Cell(5, 12).GetString());
        Assert.DoesNotContain(overview.CellsUsed(), cell => cell.GetString() == "KV26-SUPPENDING01");
        AssertSummaryValue(overview, "Bestellingen", 4m);
        AssertSummaryValue(overview, "Côte d'Or", 3m);
        AssertSummaryValue(overview, "Lotus", 4m);
        AssertSummaryValue(overview, "Totaal pakketten", 7m);
        AssertSummaryValue(overview, "Totale omzet", 69m);

        var supporters = workbook.Worksheet("Sympathisanten");
        Assert.Equal("Bevestigingsnummer", supporters.Cell(1, 1).GetString());
        Assert.Equal("Betaalstatus", supporters.Cell(1, 8).GetString());
        Assert.Equal("KV26-SUPPORTER001", supporters.Cell(2, 1).GetString());
        Assert.Equal("Sven", supporters.Cell(2, 2).GetString());
        Assert.Equal("sven@example.com", supporters.Cell(2, 3).GetString());
        Assert.Equal(2, supporters.Cell(2, 4).GetValue<int>());
        Assert.Equal(1, supporters.Cell(2, 5).GetValue<int>());
        Assert.Equal(3, supporters.Cell(2, 6).GetValue<int>());
        Assert.Equal(31m, supporters.Cell(2, 7).GetValue<decimal>());
        Assert.Equal(nameof(CookieOrderStatus.Paid), supporters.Cell(2, 8).GetString());
        Assert.False(supporters.Cell(2, 9).IsEmpty());
        Assert.DoesNotContain(supporters.CellsUsed(), cell => cell.GetString() == "KV26-STUDENT00001");
        Assert.DoesNotContain(supporters.CellsUsed(), cell => cell.GetString() == "KV26-STAFF0000001");
        Assert.DoesNotContain(supporters.CellsUsed(), cell => cell.GetString() == "KV26-SUPPENDING01");
        AssertSummaryValue(supporters, "Bestellingen", 1m);
        AssertSummaryValue(supporters, "Côte d'Or", 2m);
        AssertSummaryValue(supporters, "Lotus", 1m);
        AssertSummaryValue(supporters, "Totaal pakketten", 3m);
        AssertSummaryValue(supporters, "Omzet", 31m);

        var classSheet = workbook.Worksheet("Klas A");
        Assert.Contains(classSheet.CellsUsed(), cell => cell.GetString() == "KV26-STUDENT00001");
        Assert.Contains(classSheet.CellsUsed(), cell => cell.GetString() == "KV26-LEGACY000001");
        Assert.DoesNotContain(classSheet.CellsUsed(), cell => cell.GetString() == "KV26-SUPPORTER001");
        Assert.False(workbook.Worksheets.TryGetWorksheet("Klas", out _));

        var staffSheet = workbook.Worksheet("Personeel");
        Assert.Contains(staffSheet.CellsUsed(), cell => cell.GetString() == "KV26-STAFF0000001");
        Assert.Contains(staffSheet.CellsUsed(), cell => cell.GetString() == "SAMENVATTING PER PERSONEELSGROEP");
        Assert.DoesNotContain(staffSheet.CellsUsed(), cell => cell.GetString() == "KV26-SUPPORTER001");
        AssertSummaryValue(staffSheet, "Bestellingen", 1m);
        AssertSummaryValue(staffSheet, "Omzet", 18m);
    }

    [Fact]
    public void BuildExcel_WritesCorrectGlobalAndClassTotals()
    {
        var bytes = CookieSaleReportService.BuildExcel(
        [
            CreateOrder("KV26-AAAAAAAAAAAA", "Klas A", "Anna", 2, 1, CookieOrderStatus.Paid),
            CreateOrder("KV26-BBBBBBBBBBBB", "Klas A", "Bert", 1, 2, CookieOrderStatus.Paid),
        ]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var overview = workbook.Worksheet("Overzicht");
        Assert.Contains(overview.CellsUsed(), cell => cell.GetString() == "Bestellingen");
        Assert.Contains(overview.CellsUsed(), cell => cell.GetString() == "6");
        Assert.Contains(overview.CellsUsed(), cell => cell.GetString() == "60");

        var classSheet = workbook.Worksheet("Klas A");
        Assert.Contains(classSheet.CellsUsed(), cell => cell.GetString() == "Côte d'Or");
        Assert.Contains(classSheet.CellsUsed(), cell => cell.GetString() == "3");
        Assert.Contains(classSheet.CellsUsed(), cell => cell.GetString() == "60");
    }

    [Fact]
    public void CreateUniqueWorksheetName_SanitizesAndLimitsNames()
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Overzicht" };

        var name = CookieSaleReportService.CreateUniqueWorksheetName(
            "Een/heel*lange?klasnaam:met[tekens]123456789",
            usedNames);
        var collision = CookieSaleReportService.CreateUniqueWorksheetName(
            "Een/heel*lange?klasnaam:met[tekens]123456789",
            usedNames);

        Assert.True(name.Length <= 31);
        Assert.DoesNotContain(name, character => "[]:*?/\\".Contains(character));
        Assert.NotEqual(name, collision);
        Assert.True(collision.Length <= 31);
    }

    [Fact]
    public async Task SendDailyReportAsync_NoRecipients_SkipsEmail()
    {
        var sut = CreateSut("");

        await sut.SendDailyReportAsync();

        await _email.DidNotReceive().SendCookieSaleDailyReportAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<byte[]>(),
            Arg.Any<CookieSaleReportStats>(),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task SendDailyReportAsync_EmptyCookieRecipients_FallsBackToReportRecipients()
    {
        _storage.GetPaidCookieOrdersAsync().Returns([]);
        var sut = CreateSut("", "fallback@example.com");

        await sut.SendDailyReportAsync();

        await _email.Received(1).SendCookieSaleDailyReportAsync(
            Arg.Is<IReadOnlyList<string>>(recipients =>
                recipients.SequenceEqual(new[] { "fallback@example.com" })),
            Arg.Any<byte[]>(),
            Arg.Any<CookieSaleReportStats>(),
            Arg.Any<DateTime>());
    }

    private CookieSaleReportService CreateSut(
        string cookieRecipients,
        string reportRecipients = "") =>
        new(
            _storage,
            _email,
            Options.Create(new AppOptions
            {
                CookieReportRecipients = cookieRecipients,
                ReportRecipients = reportRecipients,
            }),
            _logger);

    private static void AssertSummaryValue(
        IXLWorksheet worksheet,
        string label,
        decimal expectedValue)
    {
        var labelCell = Assert.Single(
            worksheet.Column(7).CellsUsed().Where(
                cell => cell.Address.RowNumber > 1 && cell.GetString() == label));
        Assert.Equal(expectedValue, labelCell.CellRight().GetValue<decimal>());
    }

    private static CookieOrderEntity CreateOrder(
        string confirmationNumber,
        string className,
        string name,
        int coteDorQuantity,
        int lotusQuantity,
        CookieOrderStatus status,
        string orderType = "student",
        string staffCategory = "")
    {
        var totalPackages = coteDorQuantity + lotusQuantity;
        return new CookieOrderEntity
        {
            ConfirmationNumber = confirmationNumber,
            OrderType = orderType,
            ClassName = className,
            Name = name,
            StudentName = orderType is "staff" or "supporter" ? "" : $"Leerling {name}",
            StaffCategory = staffCategory,
            Email = $"{name.ToLowerInvariant()}@example.com",
            CoteDorQuantity = coteDorQuantity,
            LotusQuantity = lotusQuantity,
            TotalPackages = totalPackages,
            TotalAmountCents = coteDorQuantity * 1100 + lotusQuantity * 900,
            PaymentStatus = status.ToString(),
            CreatedUtc = new DateTimeOffset(2026, 9, 26, 8, 0, 0, TimeSpan.Zero),
            PaidUtc = status == CookieOrderStatus.Paid
                ? new DateTimeOffset(2026, 9, 26, 9, 0, 0, TimeSpan.Zero)
                : null,
        };
    }
}
