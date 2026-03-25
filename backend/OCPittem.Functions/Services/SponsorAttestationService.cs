using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OCPittem.Functions.Services;

public class SponsorAttestationService : ISponsorAttestationService
{
    private const string BrandColor = "#13A2A3";
    private const string Activity = "BAL PARENTAL 2026";
    private const string SignatureImageUrl = "https://stocpittem2026.blob.core.windows.net/document-assets/sponsorattest-2026.png";

    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly string[] DutchMonths =
    [
        "januari", "februari", "maart", "april", "mei", "juni",
        "juli", "augustus", "september", "oktober", "november", "december"
    ];

    public SponsorAttestationService(IHttpClientFactory httpClientFactory)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]> GenerateAttestationAsync(
        string companyName,
        string street,
        string houseNumber,
        string postalCode,
        string city,
        string enterpriseNumber,
        decimal amount,
        DateTime date)
    {
        byte[]? signatureBytes = null;
        try
        {
            var client = _httpClientFactory.CreateClient();
            signatureBytes = await client.GetByteArrayAsync(SignatureImageUrl);
        }
        catch
        {
            // Continue without signature if download fails
        }

        var dateLabel = $"{date.Day} {DutchMonths[date.Month - 1]} {date.Year}";
        var addressLine = $"{street} {houseNumber}, {postalCode} {city}";
        var vatNumber = $"BE {enterpriseNumber}";
        var amountFormatted = "€ " + amount.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Oudercomité met Pit").FontSize(20).Bold().FontColor(BrandColor);
                            col.Item().Text("Pittem").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    header.Item().PaddingTop(8).Height(2).Background(BrandColor);
                });

                page.Content().PaddingTop(30).Column(content =>
                {
                    content.Item().AlignCenter()
                        .Text("SPONSORATTEST 2026")
                        .FontSize(18).Bold().FontColor(BrandColor);

                    content.Item().PaddingTop(24).Text(t =>
                    {
                        t.Span("Oudercomité met Pit").Bold();
                        t.Span(" bevestigt hiermee dat onderstaande sponsor een financiële bijdrage heeft geleverd voor volgende activiteit:");
                    });

                    content.Item().PaddingTop(24).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(150);
                            columns.RelativeColumn();
                        });

                        void Row(string label, string value, bool shaded = false)
                        {
                            var bg = shaded ? "#f0fafa" : "#ffffff";
                            table.Cell().Background(bg).Padding(8).Text(label).Bold();
                            table.Cell().Background(bg).Padding(8).Text(value);
                        }

                        Row("Activiteit:", Activity, true);
                        Row("Datum:", dateLabel);
                        Row("Naam sponsor:", companyName, true);
                        Row("Adres:", addressLine);
                        Row("BTW nr:", vatNumber, true);
                        Row("Bedrag:", amountFormatted);
                    });

                    content.Item().PaddingTop(36).Text($"Opgemaakt te Pittem op {dateLabel}.");

                    content.Item().PaddingTop(40).Text("Namens Oudercomité met Pit,").Italic();

                    if (signatureBytes != null)
                        content.Item().PaddingTop(8).MaxWidth(180).Image(signatureBytes);
                    else
                        content.Item().PaddingTop(60).Width(200).Height(1).Background(Colors.Grey.Darken2);

                    content.Item().PaddingTop(8).Text("Het bestuur").Bold();
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().Height(1).Background(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(6).AlignCenter()
                        .Text("ocpittem.be").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}
