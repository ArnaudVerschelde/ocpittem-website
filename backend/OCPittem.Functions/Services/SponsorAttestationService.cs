using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OCPittem.Functions.Services;

public class SponsorAttestationService : ISponsorAttestationService
{
    private const string Activity = "BAL PARENTAL 2026";
    private const string SignatureImageUrl = "https://stocpittem2026.blob.core.windows.net/document-assets/sponsorattest-2026.png";

    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly string[] DutchMonths =
    [
        "januari", 
        "februari", 
        "maart", 
        "april", 
        "mei", 
        "juni",
        "juli", 
        "augustus", 
        "september", 
        "oktober", 
        "november", 
        "december"
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
        var vatNumber = $"BE {enterpriseNumber}";
        var amountFormatted = "€ " + amount.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(60);
                page.MarginHorizontal(70);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(content =>
                {
                    // 1. Briefhoofd — organisatieadres linksboven
                    content.Item().Text(t =>
                    {
                        t.Line("Oudercomité met Pit!").Bold().FontSize(12);
                        t.Line("p/a Basisschool PIT");
                        t.Line("Koolskampstraat 4");
                        t.Line("8740 Pittem");
                    });

                    // 2. Datum rechts uitgelijnd
                    content.Item().PaddingTop(20).AlignRight()
                        .Text($"Pittem, {dateLabel}");

                    // 3. Titel
                    content.Item().PaddingTop(28).AlignCenter()
                        .Text("ONTVANGSTBEWIJS SPONSORING/PUBLICITEIT")
                        .Bold().Underline();

                    // 4. Inleidende zin
                    content.Item().PaddingTop(24).Text(
                        "Ondergetekende, De Neve Jolien, voorzitter van het Oudercomité met Pit, " +
                        "verklaart hierbij te hebben ontvangen:");

                    // 5. Bedrag
                    content.Item().PaddingTop(20).Text(t =>
                    {
                        t.Span("Producten ter waarde van/een bedrag van:   ");
                        t.Span(amountFormatted).Bold();
                    });

                    // 6. Van: — bedrijfsgegevens
                    content.Item().PaddingTop(6).Row(row =>
                    {
                        row.ConstantItem(50).PaddingTop(2).Text("Van:").Bold();
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName).Bold();
                            col.Item().Text($"{street} {houseNumber}, {postalCode} {city}");
                            col.Item().Text(vatNumber);
                        });
                    });

                    // 7. Slottekst
                    content.Item().PaddingTop(20).Text(
                        $"als sponsoring voor {Activity}, georganiseerd door het Oudercomité met Pit " +
                        "op datum van 20 juni 2026.");
                    content.Item().PaddingTop(10).Text(
                        "In ruil wordt het logo/advertentie van de firma getoond tijdens het evenement.");

                    // 8. Handtekening — afbeelding of stippellijn
                    if (signatureBytes != null)
                        content.Item().PaddingTop(60).MaxWidth(220).Image(signatureBytes);
                    else
                        content.Item().PaddingTop(60).Text("…………………………………………………..").FontSize(12);

                    content.Item().PaddingTop(4).Text("namens het Oudercomité met Pit");
                });
            });
        });

        return document.GeneratePdf();
    }
}
