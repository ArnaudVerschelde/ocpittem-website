using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

public class DailyReportService : IDailyReportService
{
    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly AppOptions _appOptions;
    private readonly ILogger<DailyReportService> _logger;

    public DailyReportService(
        IStorageService storage,
        IEmailService email,
        IOptions<AppOptions> appOptions,
        ILogger<DailyReportService> logger)
    {
        _storage = storage;
        _email = email;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task SendDailyReportAsync()
    {
        var recipients = _appOptions.GetReportRecipients();
        if (recipients.Count == 0)
        {
            _logger.LogWarning("DailyReport: no recipients configured in App__ReportRecipients.");
            return;
        }

        var reportDate = DateTime.UtcNow;

        var orders = await _storage.GetAllOrdersAsync();
        var sponsors = await _storage.GetAllSponsorRequestsAsync();

        var paidOrders = orders.Where(o => o.Status == nameof(OrderStatus.Paid)).ToList();

        var paidSponsors = sponsors.Where(s => s.Status == "Paid").ToList();

        var stats = new DailyReportStats(
            TotalOrders: orders.Count,
            PaidOrders: paidOrders.Count,
            TotalToegangstickets: paidOrders.Sum(o => o.ToegangsticketCount),
            TotalEtenPartyTickets: paidOrders.Sum(o => o.EtenPartyCount),
            TotalVegetarisch: paidOrders.Sum(o => o.VegetarischCount),
            TotalDrankkaart10: paidOrders.Sum(o => o.Drankkaart10Count),
            TotalDrankkaart20: paidOrders.Sum(o => o.Drankkaart20Count),
            TotalRevenue: paidOrders.Sum(o =>
                (decimal)(o.ToegangsticketCount * 8 + o.EtenPartyCount * 50 +
                          o.Drankkaart10Count * 10 + o.Drankkaart20Count * 20)),
            TotalSponsorRequests: sponsors.Count,
            PaidSponsorOrders: paidSponsors.Count,
            TotalSponsorBrons: paidSponsors.Count(s => s.Package.Equals("brons", StringComparison.OrdinalIgnoreCase)),
            TotalSponsorZilver: paidSponsors.Count(s => s.Package.Equals("zilver", StringComparison.OrdinalIgnoreCase)),
            TotalSponsorGoud: paidSponsors.Count(s => s.Package.Equals("goud", StringComparison.OrdinalIgnoreCase)),
            TotalSponsorExtraEtenParty: paidSponsors.Sum(s => s.ExtraEtenPartyCount),
            TotalSponsorExtraVegetarisch: paidSponsors.Sum(s => s.ExtraVegetarischCount),
            TotalSponsorExtraDrankkaart20: paidSponsors.Sum(s => s.ExtraDrankkaart20Count),
            TotalSponsorRevenue: paidSponsors.Sum(s => SponsorPackagePrice(s.Package) + s.ExtraEtenPartyCount * 50m + s.ExtraDrankkaart20Count * 20m));

        var excelBytes = BuildExcel(orders, sponsors, reportDate);

        await _email.SendDailyReportAsync(recipients, excelBytes, stats, reportDate);

        _logger.LogInformation(
            "Daily report sent: {PaidOrders}/{TotalOrders} paid orders, €{Revenue} revenue, {Sponsors} sponsor requests.",
            stats.PaidOrders, stats.TotalOrders, stats.TotalRevenue, stats.TotalSponsorRequests);
    }

    private static byte[] BuildExcel(
        IReadOnlyList<OrderEntity> orders,
        IReadOnlyList<SponsorRequestEntity> sponsors,
        DateTime reportDate)
    {
        using var workbook = new XLWorkbook();

        BuildOrdersSheet(workbook, orders);
        BuildSponsorsSheet(workbook, sponsors);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildOrdersSheet(XLWorkbook workbook, IReadOnlyList<OrderEntity> orders)
    {
        var ws = workbook.Worksheets.Add("Bestellingen");

        string[] headers = ["#", "Naam", "E-mail", "Status", "Toegang", "Eten & Party", "Veg.", "Drankkaart €10", "Drankkaart €20", "Totaal (€)", "Besteld op (UTC)"];
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws.Cell(1, col);
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#13A2A3");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        ws.SheetView.FreezeRows(1);

        int row = 2;
        foreach (var order in orders.OrderBy(o => o.CreatedAt))
        {
            var total = order.ToegangsticketCount * 8 + order.EtenPartyCount * 50
                      + order.Drankkaart10Count * 10 + order.Drankkaart20Count * 20;

            ws.Cell(row, 1).Value = row - 1;
            ws.Cell(row, 2).Value = order.Name;
            ws.Cell(row, 3).Value = order.Email;
            ws.Cell(row, 4).Value = order.Status;
            ws.Cell(row, 5).Value = order.ToegangsticketCount;
            ws.Cell(row, 6).Value = order.EtenPartyCount;
            ws.Cell(row, 7).Value = order.VegetarischCount;
            ws.Cell(row, 8).Value = order.Drankkaart10Count;
            ws.Cell(row, 9).Value = order.Drankkaart20Count;
            ws.Cell(row, 10).Value = total;
            ws.Cell(row, 11).Value = order.CreatedAt.ToString("dd/MM/yyyy HH:mm");

            var rowFill = order.Status switch
            {
                nameof(OrderStatus.Paid) => XLColor.FromHtml("#ECFDF5"),
                nameof(OrderStatus.Failed) => XLColor.FromHtml("#FEF2F2"),
                _ => XLColor.FromHtml("#FFFBEB")
            };
            ws.Row(row).Style.Fill.BackgroundColor = rowFill;

            row++;
        }

        if (row > 2)
        {
            var totalsRow = row;
            ws.Cell(totalsRow, 1).Value = "TOTAAL (betaald)";
            ws.Range(totalsRow, 1, totalsRow, 4).Merge();
            ws.Cell(totalsRow, 5).FormulaA1 = $"=SUMIF(D2:D{row - 1},\"Paid\",E2:E{row - 1})";
            ws.Cell(totalsRow, 6).FormulaA1 = $"=SUMIF(D2:D{row - 1},\"Paid\",F2:F{row - 1})";
            ws.Cell(totalsRow, 7).FormulaA1 = $"=SUMIF(D2:D{row - 1},\"Paid\",G2:G{row - 1})";
            ws.Cell(totalsRow, 8).FormulaA1 = $"=SUMIF(D2:D{row - 1},\"Paid\",H2:H{row - 1})";
            ws.Cell(totalsRow, 9).FormulaA1 = $"=SUMIF(D2:D{row - 1},\"Paid\",I2:I{row - 1})";
            ws.Cell(totalsRow, 10).FormulaA1 = $"=SUMIF(D2:D{row - 1},\"Paid\",J2:J{row - 1})";
            ws.Row(totalsRow).Style.Font.Bold = true;
            ws.Row(totalsRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F7F7");
        }

        ws.Columns().AdjustToContents();
        ws.Column(3).Width = 30;
    }

    private static decimal SponsorPackagePrice(string package) => package.ToLower() switch
    {
        "brons" => 100m, "zilver" => 250m, "goud" => 500m, _ => 0m
    };

    private static void BuildSponsorsSheet(XLWorkbook workbook, IReadOnlyList<SponsorRequestEntity> sponsors)
    {
        var ws = workbook.Worksheets.Add("Sponsoren");

        string[] headers = ["#", "Bedrijf", "Contactpersoon", "E-mail", "Telefoon", "Pakket", "Status", "Extra E&P", "Extra Veg.", "Extra Drank \u20ac20", "Totaal (\u20ac)", "Inbegrepen veg.", "Ondernemingsnr.", "Straat", "Nr.", "Postcode", "Gemeente", "Aanwezig", "Aantal aanwezigen", "Aangevraagd op (UTC)", "Logo"];
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws.Cell(1, col);
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#13A2A3");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        ws.SheetView.FreezeRows(1);

        int row = 2;
        foreach (var sponsor in sponsors.OrderBy(s => s.CreatedAt))
        {
            var total = (double)(SponsorPackagePrice(sponsor.Package) + sponsor.ExtraEtenPartyCount * 50m + sponsor.ExtraDrankkaart20Count * 20m);

            ws.Cell(row, 1).Value = row - 1;
            ws.Cell(row, 2).Value = sponsor.CompanyName;
            ws.Cell(row, 3).Value = sponsor.ContactName;
            ws.Cell(row, 4).Value = sponsor.Email;
            ws.Cell(row, 5).Value = sponsor.Phone;
            ws.Cell(row, 6).Value = sponsor.Package;
            ws.Cell(row, 7).Value = sponsor.Status;
            ws.Cell(row, 8).Value = sponsor.ExtraEtenPartyCount;
            ws.Cell(row, 9).Value = sponsor.ExtraVegetarischCount;
            ws.Cell(row, 10).Value = sponsor.ExtraDrankkaart20Count;
            ws.Cell(row, 11).Value = total;
            ws.Cell(row, 12).Value = sponsor.IncludedVegetarischCount;
            ws.Cell(row, 13).Value = sponsor.EnterpriseNumber;
            ws.Cell(row, 14).Value = sponsor.Street;
            ws.Cell(row, 15).Value = sponsor.HouseNumber;
            ws.Cell(row, 16).Value = sponsor.PostalCode;
            ws.Cell(row, 17).Value = sponsor.City;
            ws.Cell(row, 18).Value = sponsor.SponsorAttends ? "Ja" : "Nee";
            ws.Cell(row, 19).Value = sponsor.SponsorAttendeesCount;
            ws.Cell(row, 20).Value = sponsor.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            if (!string.IsNullOrEmpty(sponsor.LogoUrl))
            {
                ws.Cell(row, 21).Value = "Bekijk logo";
                ws.Cell(row, 21).SetHyperlink(new XLHyperlink(sponsor.LogoUrl));
                ws.Cell(row, 21).Style.Font.FontColor = XLColor.Blue;
                ws.Cell(row, 21).Style.Font.Underline = XLFontUnderlineValues.Single;
            }

            var rowFill = sponsor.Status switch
            {
                "Paid" => XLColor.FromHtml("#ECFDF5"),
                "Failed" => XLColor.FromHtml("#FEF2F2"),
                _ => XLColor.FromHtml("#FFFBEB")
            };
            ws.Row(row).Style.Fill.BackgroundColor = rowFill;

            row++;
        }

        if (row > 2)
        {
            var totalsRow = row;
            ws.Cell(totalsRow, 1).Value = "TOTAAL (betaald)";
            ws.Range(totalsRow, 1, totalsRow, 7).Merge();
            ws.Cell(totalsRow, 8).FormulaA1 = $"=SUMIF(G2:G{row - 1},\"Paid\",H2:H{row - 1})";
            ws.Cell(totalsRow, 9).FormulaA1 = $"=SUMIF(G2:G{row - 1},\"Paid\",I2:I{row - 1})";
            ws.Cell(totalsRow, 10).FormulaA1 = $"=SUMIF(G2:G{row - 1},\"Paid\",J2:J{row - 1})";
            ws.Cell(totalsRow, 11).FormulaA1 = $"=SUMIF(G2:G{row - 1},\"Paid\",K2:K{row - 1})";
            ws.Row(totalsRow).Style.Font.Bold = true;
            ws.Row(totalsRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F7F7");
        }

        ws.Columns().AdjustToContents();
        ws.Column(4).Width = 30;
        ws.Column(13).Width = 40;
    }
}
