using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class AdminResendSponsorEmailFunction
{
    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly AppOptions _appOptions;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AdminResendSponsorEmailFunction> _logger;

    public AdminResendSponsorEmailFunction(
        IStorageService storage,
        IEmailService email,
        IOptions<AppOptions> appOptions,
        BlobServiceClient blobServiceClient,
        ILogger<AdminResendSponsorEmailFunction> logger)
    {
        _storage = storage;
        _email = email;
        _appOptions = appOptions.Value;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    [Function("AdminResendSponsorEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "manage/sponsors/resend-email")] HttpRequest req)
    {
        var requestId = req.Query["requestId"].ToString();
        if (string.IsNullOrWhiteSpace(requestId))
            return new BadRequestObjectResult(new { error = "Query parameter 'requestId' is required." });

        var overrideEmail = req.Query["overrideEmail"].ToString();
        var isTestMode = !string.IsNullOrWhiteSpace(overrideEmail);

        var sponsor = await _storage.GetSponsorRequestByIdAsync(requestId);
        if (sponsor == null)
            return new NotFoundObjectResult(new { error = $"Sponsor request '{requestId}' not found." });

        if (sponsor.Status != "Paid")
            return new BadRequestObjectResult(new { error = $"Sponsor request is not Paid (current status: {sponsor.Status})." });

        var ticketEntities = await _storage.GetTicketsByOrderIdAsync(requestId);
        var tickets = ticketEntities
            .Select(t => new TicketPdfData(t.RowKey, t.QrPayload, t.TicketType, t.IsVegetarisch))
            .ToList();

        var pdfBytes = await TryDownloadAsync(sponsor.PdfBlobUrl);
        var attestBytes = await TryDownloadAsync(sponsor.AttestationBlobUrl);

        var recipientEmail = isTestMode ? overrideEmail : sponsor.Email;

        await _email.SendSponsorPaymentConfirmationAsync(
            recipientEmail, sponsor.CompanyName, sponsor.Package,
            sponsor.ExtraEtenPartyCount, sponsor.ExtraVegetarischCount, sponsor.ExtraDrankkaart20Count,
            sponsor.IncludedVegetarischCount,
            tickets, pdfBytes, attestBytes);

        var contactEmail = isTestMode
            ? overrideEmail
            : string.IsNullOrEmpty(_appOptions.ContactEmail)
                ? "oudercomitepittem@gmail.com"
                : _appOptions.ContactEmail;

        await _email.SendContactNotificationAsync(
            sponsor.CompanyName, sponsor.Email,
            $"Sponsorpakket betaald: {sponsor.Package} \u2014 {sponsor.CompanyName}",
            $"Bedrijf: {sponsor.CompanyName}\nOndernemingsnummer: {sponsor.EnterpriseNumber}\nAdres: {sponsor.Street} {sponsor.HouseNumber}, {sponsor.PostalCode} {sponsor.City}\nContactpersoon: {sponsor.ContactName}\nPakket: {sponsor.Package}\nExtra Eten & Party: {sponsor.ExtraEtenPartyCount}\nExtra Drankkaarten \u20ac20: {sponsor.ExtraDrankkaart20Count}\nSponsor aanwezig: {(sponsor.SponsorAttends ? "Ja" : "Nee")}\nAantal aanwezigen: {sponsor.SponsorAttendeesCount}",
            contactEmail);

        if (isTestMode)
            _logger.LogInformation("Admin resent sponsor emails for request {RequestId} ({Company}) in TEST MODE to {Override}", requestId, sponsor.CompanyName, overrideEmail);
        else
            _logger.LogInformation("Admin resent sponsor emails for request {RequestId} ({Company})", requestId, sponsor.CompanyName);

        return new OkObjectResult(new
        {
            message = $"Emails successfully resent for {sponsor.CompanyName}.",
            recipient = recipientEmail,
            testMode = isTestMode
        });
    }

    private async Task<byte[]?> TryDownloadAsync(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.TrimStart('/');
            var slashIndex = path.IndexOf('/');
            if (slashIndex < 0)
            {
                _logger.LogWarning("Could not parse container/blob from URL {Url}", url);
                return null;
            }

            var containerName = path[..slashIndex];
            var blobName = path[(slashIndex + 1)..];

            var blobClient = _blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync();
            return response.Value.Content.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download blob from {Url}", url);
            return null;
        }
    }
}
