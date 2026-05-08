using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using OCPittem.Functions.Validators;

namespace OCPittem.Functions.Functions;

public class AdminCreateAndPaySponsorFunction
{
    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly ITicketPdfService _ticketPdf;
    private readonly ISponsorAttestationService _attestation;
    private readonly AppOptions _appOptions;
    private readonly ILogger<AdminCreateAndPaySponsorFunction> _logger;

    public AdminCreateAndPaySponsorFunction(
        IStorageService storage,
        IEmailService email,
        ITicketPdfService ticketPdf,
        ISponsorAttestationService attestation,
        IOptions<AppOptions> appOptions,
        ILogger<AdminCreateAndPaySponsorFunction> logger)
    {
        _storage = storage;
        _email = email;
        _ticketPdf = ticketPdf;
        _attestation = attestation;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    [Function("AdminCreateAndPaySponsor")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "manage/sponsors/create-paid")] HttpRequest req)
    {
        var overrideEmail = req.Query["overrideEmail"].ToString();
        var isTestMode = !string.IsNullOrWhiteSpace(overrideEmail);

        SponsorRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<SponsorRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Ongeldig JSON-verzoek." });
        }

        if (body == null
            || string.IsNullOrWhiteSpace(body.CompanyName)
            || string.IsNullOrWhiteSpace(body.ContactName)
            || string.IsNullOrWhiteSpace(body.Email)
            || string.IsNullOrWhiteSpace(body.Package)
            || string.IsNullOrWhiteSpace(body.EnterpriseNumber)
            || string.IsNullOrWhiteSpace(body.Street)
            || string.IsNullOrWhiteSpace(body.HouseNumber)
            || string.IsNullOrWhiteSpace(body.PostalCode)
            || string.IsNullOrWhiteSpace(body.City))
        {
            return new BadRequestObjectResult(new { error = "Vul alle verplichte velden in." });
        }

        if (!BelgianEnterpriseNumberValidator.IsValid(body.EnterpriseNumber))
            return new BadRequestObjectResult(new { error = "Ongeldig Belgisch ondernemingsnummer." });

        if (!System.Text.RegularExpressions.Regex.IsMatch(body.PostalCode.Trim(), @"^\d{4}$"))
            return new BadRequestObjectResult(new { error = "Ongeldige Belgische postcode (4 cijfers verwacht)." });

        var validPackages = new[] { "brons", "zilver", "goud" };
        if (!validPackages.Contains(body.Package.ToLowerInvariant()))
            return new BadRequestObjectResult(new { error = "Ongeldig sponsorpakket. Kies uit: brons, zilver, goud." });

        if (body.ExtraEtenPartyCount < 0 || body.ExtraVegetarischCount < 0
            || body.ExtraDrankkaart20Count < 0 || body.IncludedVegetarischCount < 0)
            return new BadRequestObjectResult(new { error = "Ongeldige aantallen." });

        if (body.ExtraVegetarischCount > body.ExtraEtenPartyCount)
            return new BadRequestObjectResult(new { error = "Aantal vegetarische opties mag niet groter zijn dan het aantal extra tickets." });

        var includedTicketCount = body.Package.ToLowerInvariant() switch { "zilver" => 2, "goud" => 4, _ => 0 };
        if (body.IncludedVegetarischCount > includedTicketCount)
            return new BadRequestObjectResult(new { error = "Aantal vegetarische opties mag niet groter zijn dan het aantal inbegrepen tickets." });

        var requestId = Guid.NewGuid().ToString();

        var entity = new SponsorRequestEntity
        {
            PartitionKey = "Sponsor",
            RowKey = requestId,
            Status = "Paid",
            CompanyName = body.CompanyName.Trim(),
            ContactName = body.ContactName.Trim(),
            Email = body.Email.Trim(),
            Phone = body.Phone?.Trim() ?? "",
            Package = body.Package.ToLowerInvariant(),
            Message = "",
            EnterpriseNumber = BelgianEnterpriseNumberValidator.Normalize(body.EnterpriseNumber),
            Street = body.Street.Trim(),
            HouseNumber = body.HouseNumber.Trim(),
            PostalCode = body.PostalCode.Trim(),
            City = body.City.Trim(),
            ExtraEtenPartyCount = body.ExtraEtenPartyCount,
            ExtraVegetarischCount = body.ExtraVegetarischCount,
            ExtraDrankkaart20Count = body.ExtraDrankkaart20Count,
            IncludedVegetarischCount = body.IncludedVegetarischCount,
            SponsorAttends = body.SponsorAttends,
            SponsorAttendeesCount = body.SponsorAttends ? body.SponsorAttendeesCount : 0,
            LogoUrl = body.LogoUrl?.Trim() ?? "",
        };

        if (!isTestMode)
        {
            await _storage.SaveSponsorRequestAsync(entity);
            _logger.LogInformation("Manually created sponsor request {RequestId} ({Company}, {Package})",
                requestId, entity.CompanyName, entity.Package);
        }
        else
        {
            _logger.LogInformation(
                "[TEST] Dry-run create for {Company} ({Package}) — geen opslag in storage",
                entity.CompanyName, entity.Package);
        }

        // Tickets aanmaken
        var pdfTickets = new List<TicketPdfData>();

        for (int i = 0; i < includedTicketCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = QrPayloadHelper.Generate(ticketId, _appOptions.TicketHmacSecret);
            var isVeg = i < body.IncludedVegetarischCount;
            if (!isTestMode)
                await _storage.SaveTicketAsync(new TicketEntity
                {
                    PartitionKey = requestId, RowKey = ticketId,
                    QrPayload = qrPayload, TicketType = nameof(TicketKind.EtenParty), IsVegetarisch = isVeg,
                });
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        for (int i = 0; i < body.ExtraEtenPartyCount; i++)
        {
            var ticketId = Guid.NewGuid().ToString();
            var qrPayload = QrPayloadHelper.Generate(ticketId, _appOptions.TicketHmacSecret);
            var isVeg = i < body.ExtraVegetarischCount;
            if (!isTestMode)
                await _storage.SaveTicketAsync(new TicketEntity
                {
                    PartitionKey = requestId, RowKey = ticketId,
                    QrPayload = qrPayload, TicketType = nameof(TicketKind.EtenParty), IsVegetarisch = isVeg,
                });
            pdfTickets.Add(new TicketPdfData(ticketId, qrPayload, nameof(TicketKind.EtenParty), isVeg));
        }

        // Ticket PDF genereren en opslaan
        byte[]? combinedPdf = pdfTickets.Count > 0
            ? _ticketPdf.GenerateTicketsPdf(pdfTickets, entity.CompanyName, "Bal Parental 2026")
            : null;

        if (combinedPdf != null && !isTestMode)
        {
            var blobUrl = await _storage.SaveTicketPdfAsync(requestId, combinedPdf);
            entity.PdfBlobUrl = blobUrl;
            _logger.LogInformation("Ticket PDF saved for sponsor request {RequestId}", requestId);
        }

        // Sponsorattest genereren en opslaan
        var packagePrice = entity.Package switch
        {
            "brons" => 100m, "zilver" => 250m, "goud" => 500m, _ => 0m
        };
        var total = packagePrice + body.ExtraEtenPartyCount * 50m + body.ExtraDrankkaart20Count * 20m;

        byte[]? attestPdf = null;
        try
        {
            attestPdf = await _attestation.GenerateAttestationAsync(
                entity.CompanyName, entity.Street, entity.HouseNumber,
                entity.PostalCode, entity.City, entity.EnterpriseNumber,
                total, DateTime.UtcNow);
            if (!isTestMode)
            {
                var attestUrl = await _storage.SaveSponsorAttestationAsync(requestId, attestPdf);
                entity.AttestationBlobUrl = attestUrl;
                _logger.LogInformation("Attestation PDF saved for sponsor request {RequestId}", requestId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate/save attestation for request {RequestId}", requestId);
        }

        if (!isTestMode)
            await _storage.UpdateSponsorRequestAsync(entity);

        var recipientEmail = isTestMode ? overrideEmail : entity.Email;

        await _email.SendSponsorPaymentConfirmationAsync(
            recipientEmail, entity.CompanyName, entity.Package,
            entity.ExtraEtenPartyCount, entity.ExtraVegetarischCount, entity.ExtraDrankkaart20Count,
            entity.IncludedVegetarischCount,
            pdfTickets, combinedPdf, attestPdf);

        var contactEmail = isTestMode
            ? overrideEmail
            : string.IsNullOrEmpty(_appOptions.ContactEmail)
                ? "oudercomitepittem@gmail.com"
                : _appOptions.ContactEmail;

        await _email.SendContactNotificationAsync(
            entity.CompanyName, entity.Email,
            $"Sponsorpakket betaald (manueel aangemaakt): {entity.Package} \u2014 {entity.CompanyName}",
            $"Bedrijf: {entity.CompanyName}\nOndernemingsnummer: {entity.EnterpriseNumber}\nAdres: {entity.Street} {entity.HouseNumber}, {entity.PostalCode} {entity.City}\nContactpersoon: {entity.ContactName}\nPakket: {entity.Package}\nExtra Eten & Party: {entity.ExtraEtenPartyCount}\nExtra Drankkaarten \u20ac20: {entity.ExtraDrankkaart20Count}\nSponsor aanwezig: {(entity.SponsorAttends ? "Ja" : "Nee")}\nAantal aanwezigen: {entity.SponsorAttendeesCount}",
            contactEmail);

        _logger.LogInformation(
            "Sponsor {RequestId} ({Company}) manually created and marked as Paid{TestMode}",
            requestId, entity.CompanyName, isTestMode ? $" [TEST → {overrideEmail}]" : "");

        return new OkObjectResult(new
        {
            message = isTestMode
                ? $"[TEST] Dry-run voltooid voor {entity.CompanyName} — geen opslag, e-mail verstuurd naar {overrideEmail}."
                : $"Sponsor {entity.CompanyName} successfully created and marked as Paid.",
            requestId = isTestMode ? (string?)null : requestId,
            ticketsGenerated = pdfTickets.Count,
            attestationGenerated = attestPdf != null,
            recipient = recipientEmail,
            testMode = isTestMode
        });
    }
}
