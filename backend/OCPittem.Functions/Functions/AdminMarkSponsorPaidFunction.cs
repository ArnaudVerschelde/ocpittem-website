using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class AdminMarkSponsorPaidFunction
{
    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly ITicketPdfService _ticketPdf;
    private readonly ISponsorAttestationService _attestation;
    private readonly AppOptions _appOptions;
    private readonly ILogger<AdminMarkSponsorPaidFunction> _logger;

    public AdminMarkSponsorPaidFunction(
        IStorageService storage,
        IEmailService email,
        ITicketPdfService ticketPdf,
        ISponsorAttestationService attestation,
        IOptions<AppOptions> appOptions,
        ILogger<AdminMarkSponsorPaidFunction> logger)
    {
        _storage = storage;
        _email = email;
        _ticketPdf = ticketPdf;
        _attestation = attestation;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    [Function("AdminMarkSponsorPaid")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "manage/sponsors/mark-paid")] HttpRequest req)
    {
        var requestId = req.Query["requestId"].ToString();
        if (string.IsNullOrWhiteSpace(requestId))
            return new BadRequestObjectResult(new { error = "Query parameter 'requestId' is required." });

        var overrideEmail = req.Query["overrideEmail"].ToString();
        var isTestMode = !string.IsNullOrWhiteSpace(overrideEmail);

        var sponsor = await _storage.GetSponsorRequestByIdAsync(requestId);
        if (sponsor == null)
            return new NotFoundObjectResult(new { error = $"Sponsor request '{requestId}' not found." });

        if (sponsor.Status == "Paid")
            return new BadRequestObjectResult(new { error = $"Sponsor request '{requestId}' is already Paid." });

        sponsor.Status = "Paid";

        // Tickets aanmaken
        var includedTickets = sponsor.Package.ToLowerInvariant() switch
        {
            "zilver" => 2, "goud" => 4, _ => 0
        };

        var pdfTickets = new List<TicketPdfData>();

        for (int i = 0; i < includedTickets; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = QrPayloadHelper.Generate(ticketId, _appOptions.TicketHmacSecret);
            var isVeg = i < sponsor.IncludedVegetarischCount;
            var ticket = new TicketEntity
            {
                PartitionKey = requestId, RowKey = ticketId,
                QrPayload = qrPayload, TicketType = nameof(TicketKind.EtenParty), IsVegetarisch = isVeg,
            };
            await _storage.SaveTicketAsync(ticket);
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        for (int i = 0; i < sponsor.ExtraEtenPartyCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = QrPayloadHelper.Generate(ticketId, _appOptions.TicketHmacSecret);
            var isVeg = i < sponsor.ExtraVegetarischCount;
            var ticket = new TicketEntity
            {
                PartitionKey = requestId, RowKey = ticketId,
                QrPayload = qrPayload, TicketType = nameof(TicketKind.EtenParty), IsVegetarisch = isVeg,
            };
            await _storage.SaveTicketAsync(ticket);
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        // Ticket PDF genereren en opslaan
        byte[]? combinedPdf = pdfTickets.Count > 0
            ? _ticketPdf.GenerateTicketsPdf(pdfTickets, sponsor.CompanyName, "Bal Parental 2026")
            : null;

        if (combinedPdf != null)
        {
            var blobUrl = await _storage.SaveTicketPdfAsync(requestId, combinedPdf);
            sponsor.PdfBlobUrl = blobUrl;
            _logger.LogInformation("Ticket PDF saved for sponsor request {RequestId}", requestId);
        }

        // Sponsorattest genereren en opslaan
        var packagePrice = sponsor.Package.ToLowerInvariant() switch
        {
            "brons" => 100m, "zilver" => 250m, "goud" => 500m, _ => 0m
        };
        var total = packagePrice + sponsor.ExtraEtenPartyCount * 50m + sponsor.ExtraDrankkaart20Count * 20m;

        byte[]? attestPdf = null;
        try
        {
            attestPdf = await _attestation.GenerateAttestationAsync(
                sponsor.CompanyName, sponsor.Street, sponsor.HouseNumber,
                sponsor.PostalCode, sponsor.City, sponsor.EnterpriseNumber,
                total, DateTime.UtcNow);
            var attestUrl = await _storage.SaveSponsorAttestationAsync(requestId, attestPdf);
            sponsor.AttestationBlobUrl = attestUrl;
            _logger.LogInformation("Attestation PDF saved for sponsor request {RequestId}", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate/save attestation for request {RequestId}", requestId);
        }

        await _storage.UpdateSponsorRequestAsync(sponsor);

        var recipientEmail = isTestMode ? overrideEmail : sponsor.Email;

        await _email.SendSponsorPaymentConfirmationAsync(
            recipientEmail, sponsor.CompanyName, sponsor.Package,
            sponsor.ExtraEtenPartyCount, sponsor.ExtraVegetarischCount, sponsor.ExtraDrankkaart20Count,
            sponsor.IncludedVegetarischCount,
            pdfTickets, combinedPdf, attestPdf);

        var contactEmail = isTestMode
            ? overrideEmail
            : string.IsNullOrEmpty(_appOptions.ContactEmail)
                ? "oudercomitepittem@gmail.com"
                : _appOptions.ContactEmail;

        await _email.SendContactNotificationAsync(
            sponsor.CompanyName, sponsor.Email,
            $"Sponsorpakket betaald (manueel): {sponsor.Package} \u2014 {sponsor.CompanyName}",
            $"Bedrijf: {sponsor.CompanyName}\nOndernemingsnummer: {sponsor.EnterpriseNumber}\nAdres: {sponsor.Street} {sponsor.HouseNumber}, {sponsor.PostalCode} {sponsor.City}\nContactpersoon: {sponsor.ContactName}\nPakket: {sponsor.Package}\nExtra Eten & Party: {sponsor.ExtraEtenPartyCount}\nExtra Drankkaarten \u20ac20: {sponsor.ExtraDrankkaart20Count}\nSponsor aanwezig: {(sponsor.SponsorAttends ? "Ja" : "Nee")}\nAantal aanwezigen: {sponsor.SponsorAttendeesCount}",
            contactEmail);

        _logger.LogInformation(
            "Sponsor {RequestId} ({Company}) manually marked as Paid{TestMode}",
            requestId, sponsor.CompanyName, isTestMode ? $" [TEST → {overrideEmail}]" : "");

        return new OkObjectResult(new
        {
            message = $"Sponsor {sponsor.CompanyName} successfully marked as Paid.",
            requestId,
            ticketsGenerated = pdfTickets.Count,
            attestationGenerated = attestPdf != null,
            recipient = recipientEmail,
            testMode = isTestMode
        });
    }
}
