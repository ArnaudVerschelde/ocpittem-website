using System.Globalization;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OCPittem.Functions.Services;

public class SponsorAttestationService : ISponsorAttestationService
{
    private const string Activity = "BAL PARENTAL 2026";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<SponsorAttestationService> _logger;
    private readonly string _signatureContainerName;
    private readonly string _signatureBlobName;

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

    public SponsorAttestationService(
        BlobServiceClient blobServiceClient,
        IOptions<SponsorAttestationOptions> options,
        ILogger<SponsorAttestationService> logger)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _signatureContainerName = options.Value.SignatureContainerName;
        _signatureBlobName = options.Value.SignatureBlobName;
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
        var signatureBytes = await TryLoadSignatureAsync();

        var localDate = ConvertToBrusselsTime(date);
        var dateLabel = $"{localDate.Day} {DutchMonths[localDate.Month - 1]} {localDate.Year}";

        var vatDigits = new string((enterpriseNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        var vatNumber = string.IsNullOrWhiteSpace(vatDigits) ? string.Empty : $"BE {vatDigits}";

        var amountFormatted = "€ " + amount.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginVertical(60);
                page.MarginHorizontal(70);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Content().Column(content =>
                {
                    // Briefhoofd
                    content.Item().Text(t =>
                    {
                        t.Line("Oudercomité met Pit!").Bold().FontSize(12);
                        t.Line("p/a Basisschool PIT");
                        t.Line("Koolskampstraat 4");
                        t.Line("8740 Pittem");
                    });

                    // Datum rechts
                    content.Item()
                        .PaddingTop(20)
                        .AlignRight()
                        .Text($"Pittem, {dateLabel}");

                    // Titel
                    content.Item()
                        .PaddingTop(28)
                        .AlignCenter()
                        .Text("ONTVANGSTBEWIJS SPONSORING/PUBLICITEIT")
                        .Bold()
                        .Underline();

                    // Inleiding
                    content.Item()
                        .PaddingTop(24)
                        .Text("Ondergetekende, De Neve Jolien, voorzitter van het Oudercomité met Pit, verklaart hierbij te hebben ontvangen:");

                    // Bedrag
                    content.Item()
                        .PaddingTop(20)
                        .Text(t =>
                        {
                            t.Span("Producten ter waarde van/een bedrag van:   ");
                            t.Span(amountFormatted).Bold();
                        });

                    // Van
                    content.Item()
                        .PaddingTop(6)
                        .Row(row =>
                        {
                            row.ConstantItem(50).PaddingTop(2).Text("Van:").Bold();

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(companyName).Bold();
                                col.Item().Text($"{street} {houseNumber}, {postalCode} {city}");

                                if (!string.IsNullOrWhiteSpace(vatNumber))
                                    col.Item().Text(vatNumber);
                            });
                        });

                    // Slottekst
                    content.Item()
                        .PaddingTop(20)
                        .Text($"als sponsoring voor {Activity}, georganiseerd door het Oudercomité met Pit op datum van 20 juni 2026.");

                    content.Item()
                        .PaddingTop(10)
                        .Text("In ruil wordt het logo/advertentie van de firma getoond tijdens het evenement.");

                    // Handtekening + stippellijn eronder
                    content.Item()
                        .PaddingTop(60)
                        .Width(240)
                        .Column(sig =>
                        {
                            if (signatureBytes is not null && signatureBytes.Length > 0)
                            {
                                sig.Item()
                                    .Height(70)
                                    .Image(signatureBytes)
                                    .FitHeight();
                            }
                            else
                            {
                                sig.Item()
                                    .Height(70);
                            }

                            sig.Item()
                                .PaddingTop(2)
                                .Text("........................................................")
                                .FontSize(11);

                            sig.Item()
                                .PaddingTop(4)
                                .Text("namens het Oudercomité met Pit");
                        });
                });
            });
        });

        return document.GeneratePdf();
    }

    private async Task<byte[]?> TryLoadSignatureAsync()
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_signatureContainerName);
            var blobClient = containerClient.GetBlobClient(_signatureBlobName);

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning(
                    "Signature blob not found. Container: {Container}, Blob: {Blob}",
                    _signatureContainerName,
                    _signatureBlobName);
                return null;
            }

            var download = await blobClient.DownloadContentAsync();
            return download.Value.Content.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load signature blob. Container: {Container}, Blob: {Blob}",
                _signatureContainerName,
                _signatureBlobName);

            return null;
        }
    }

    private static DateTime ConvertToBrusselsTime(DateTime utcOrLocalDate)
    {
        var utc = utcOrLocalDate.Kind switch
        {
            DateTimeKind.Utc => utcOrLocalDate,
            DateTimeKind.Local => utcOrLocalDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcOrLocalDate, DateTimeKind.Utc)
        };

        try
        {
            // Linux
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }
        catch
        {
            // Windows fallback
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }
    }
}