using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;

namespace OCPittem.Functions.Services;

public class CookieSaleReportService : ICookieSaleReportService
{
    private static readonly char[] InvalidWorksheetNameCharacters = ['[', ']', ':', '*', '?', '/', '\\'];

    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly AppOptions _appOptions;
    private readonly ILogger<CookieSaleReportService> _logger;

    public CookieSaleReportService(
        IStorageService storage,
        IEmailService email,
        IOptions<AppOptions> appOptions,
        ILogger<CookieSaleReportService> logger)
    {
        _storage = storage;
        _email = email;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task SendDailyReportAsync()
    {
        var recipients = _appOptions.GetCookieReportRecipients();
        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "CookieSaleReport: no recipients configured in App__CookieReportRecipients or App__ReportRecipients.");
            return;
        }

        var paidOrders = (await _storage.GetPaidCookieOrdersAsync())
            .Where(order => order.PaymentStatus == nameof(CookieOrderStatus.Paid))
            .ToList();
        var stats = new CookieSaleReportStats(
            TotalOrders: paidOrders.Count,
            TotalCoteDorPackages: paidOrders.Sum(order => order.CoteDorQuantity),
            TotalLotusPackages: paidOrders.Sum(order => order.LotusQuantity),
            TotalPackages: paidOrders.Sum(order => order.TotalPackages),
            TotalAmountCents: paidOrders.Sum(order => order.TotalAmountCents));
        var reportDate = DateTime.UtcNow;
        var excelBytes = BuildExcel(paidOrders);

        await _email.SendCookieSaleDailyReportAsync(recipients, excelBytes, stats, reportDate);
        _logger.LogInformation(
            "Cookie-sale report sent with {OrderCount} paid orders and {PackageCount} packages.",
            stats.TotalOrders,
            stats.TotalPackages);
    }

    internal static byte[] BuildExcel(IReadOnlyList<CookieOrderEntity> orders)
    {
        var paidOrders = orders
            .Where(order => order.PaymentStatus == nameof(CookieOrderStatus.Paid))
            .OrderBy(order => order.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(order => order.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var workbook = new XLWorkbook();
        BuildOverviewWorksheet(workbook, paidOrders);
        BuildClassWorksheets(workbook, paidOrders);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildOverviewWorksheet(
        XLWorkbook workbook,
        IReadOnlyList<CookieOrderEntity> orders)
    {
        var worksheet = workbook.Worksheets.Add("Overzicht");
        string[] headers =
        [
            "Bevestigingsnummer",
            "Naam leerling",
            "Klas",
            "Naam besteller",
            "E-mail",
            "Côte d'Or",
            "Lotus",
            "Totaal pakketten",
            "Totaalbedrag",
            "Betaald op",
        ];
        WriteHeader(worksheet, headers);

        var row = 2;
        foreach (var order in orders)
        {
            worksheet.Cell(row, 1).Value = order.ConfirmationNumber;
            worksheet.Cell(row, 2).Value = order.StudentName;
            worksheet.Cell(row, 3).Value = order.ClassName;
            worksheet.Cell(row, 4).Value = order.Name;
            worksheet.Cell(row, 5).Value = order.Email;
            worksheet.Cell(row, 6).Value = order.CoteDorQuantity;
            worksheet.Cell(row, 7).Value = order.LotusQuantity;
            worksheet.Cell(row, 8).Value = order.TotalPackages;
            worksheet.Cell(row, 9).Value = order.TotalAmountCents / 100m;
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "€ #,##0.00";
            if (order.PaidUtc.HasValue)
            {
                worksheet.Cell(row, 10).Value = order.PaidUtc.Value.UtcDateTime;
                worksheet.Cell(row, 10).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            }
            row++;
        }

        var totalsRow = row + 1;
        worksheet.Cell(totalsRow, 1).Value = "TOTALEN";
        worksheet.Cell(totalsRow, 1).Style.Font.Bold = true;
        WriteSummaryValue(worksheet, totalsRow, 7, "Bestellingen", orders.Count);
        WriteSummaryValue(worksheet, totalsRow + 1, 7, "Côte d'Or", orders.Sum(order => order.CoteDorQuantity));
        WriteSummaryValue(worksheet, totalsRow + 2, 7, "Lotus", orders.Sum(order => order.LotusQuantity));
        WriteSummaryValue(worksheet, totalsRow + 3, 7, "Totaal pakketten", orders.Sum(order => order.TotalPackages));
        WriteSummaryValue(
            worksheet,
            totalsRow + 4,
            7,
            "Totale omzet",
            orders.Sum(order => order.TotalAmountCents) / 100m,
            currency: true);

        FormatWorksheet(worksheet, headers.Length);
    }

    private static void BuildClassWorksheets(
        XLWorkbook workbook,
        IReadOnlyList<CookieOrderEntity> orders)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Overzicht" };

        foreach (var classGroup in orders.GroupBy(order => order.ClassName, StringComparer.OrdinalIgnoreCase))
        {
            var worksheetName = CreateUniqueWorksheetName(classGroup.Key, usedNames);
            var worksheet = workbook.Worksheets.Add(worksheetName);
            string[] headers =
            [
                "Bevestigingsnummer",
                "Naam leerling",
                "Klas",
                "Naam besteller",
                "E-mail",
                "Côte d'Or",
                "Lotus",
                "Totaal pakketten",
                "Totaalbedrag",
                "Betaald op",
            ];
            WriteHeader(worksheet, headers);

            var classOrders = classGroup
                .OrderBy(order => order.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var row = 2;

            foreach (var order in classOrders)
            {
                worksheet.Cell(row, 1).Value = order.ConfirmationNumber;
                worksheet.Cell(row, 2).Value = order.StudentName;
                worksheet.Cell(row, 3).Value = order.ClassName;
                worksheet.Cell(row, 4).Value = order.Name;
                worksheet.Cell(row, 5).Value = order.Email;
                worksheet.Cell(row, 6).Value = order.CoteDorQuantity;
                worksheet.Cell(row, 7).Value = order.LotusQuantity;
                worksheet.Cell(row, 8).Value = order.TotalPackages;
                worksheet.Cell(row, 9).Value = order.TotalAmountCents / 100m;
                worksheet.Cell(row, 9).Style.NumberFormat.Format = "€ #,##0.00";
                if (order.PaidUtc.HasValue)
                {
                    worksheet.Cell(row, 10).Value = order.PaidUtc.Value.UtcDateTime;
                    worksheet.Cell(row, 10).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                }
                row++;
            }

            var totalsRow = row + 1;
            worksheet.Cell(totalsRow, 1).Value = "TOTALEN";
            worksheet.Cell(totalsRow, 1).Style.Font.Bold = true;
            WriteSummaryValue(worksheet, totalsRow, 7, "Côte d'Or", classOrders.Sum(order => order.CoteDorQuantity));
            WriteSummaryValue(worksheet, totalsRow + 1, 7, "Lotus", classOrders.Sum(order => order.LotusQuantity));
            WriteSummaryValue(worksheet, totalsRow + 2, 7, "Totaal pakketten", classOrders.Sum(order => order.TotalPackages));
            WriteSummaryValue(
                worksheet,
                totalsRow + 3,
                7,
                "Omzet",
                classOrders.Sum(order => order.TotalAmountCents) / 100m,
                currency: true);

            FormatWorksheet(worksheet, headers.Length);
        }
    }

    private static void WriteHeader(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var column = 1; column <= headers.Count; column++)
        {
            var cell = worksheet.Cell(1, column);
            cell.Value = headers[column - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#13A2A3");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        worksheet.SheetView.FreezeRows(1);
    }

    private static void WriteSummaryValue(
        IXLWorksheet worksheet,
        int row,
        int labelColumn,
        string label,
        decimal value,
        bool currency = false)
    {
        worksheet.Cell(row, labelColumn).Value = label;
        worksheet.Cell(row, labelColumn).Style.Font.Bold = true;
        worksheet.Cell(row, labelColumn + 1).Value = value;
        worksheet.Cell(row, labelColumn + 1).Style.Font.Bold = true;
        worksheet.Range(row, labelColumn, row, labelColumn + 1).Style.Fill.BackgroundColor =
            XLColor.FromHtml("#E0F7F7");

        if (currency)
            worksheet.Cell(row, labelColumn + 1).Style.NumberFormat.Format = "€ #,##0.00";
    }

    private static void FormatWorksheet(IXLWorksheet worksheet, int columnCount)
    {
        worksheet.RangeUsed()?.SetAutoFilter();
        worksheet.Columns(1, columnCount).AdjustToContents();
    }

    internal static string CreateUniqueWorksheetName(string className, ISet<string> usedNames)
    {
        var sanitized = new string(className
            .Trim()
            .Select(character => InvalidWorksheetNameCharacters.Contains(character) ? '-' : character)
            .ToArray())
            .Trim('\'');

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "Klas";

        var baseName = sanitized[..Math.Min(sanitized.Length, 31)];
        var candidate = baseName;
        var suffixNumber = 2;

        while (!usedNames.Add(candidate))
        {
            var suffix = $" ({suffixNumber++})";
            var maximumBaseLength = 31 - suffix.Length;
            candidate = $"{baseName[..Math.Min(baseName.Length, maximumBaseLength)]}{suffix}";
        }

        return candidate;
    }
}
