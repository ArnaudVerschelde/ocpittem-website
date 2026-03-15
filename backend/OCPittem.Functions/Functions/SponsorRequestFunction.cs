using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class SponsorRequestFunction
{
    private readonly IStorageService _storage;
    private readonly IEmailService _email;
    private readonly IOptions<AppOptions> _appOptions;
    private readonly ILogger<SponsorRequestFunction> _logger;

    public SponsorRequestFunction(IStorageService storage, 
        IEmailService email, 
        IOptions<AppOptions> appOptions, 
        ILogger<SponsorRequestFunction> logger)
    {
        _storage = storage;
        _email = email;
        _appOptions = appOptions;
        _logger = logger;
    }

    [Function("SponsorRequest")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sponsors/request")] HttpRequest req)
    {
        SponsorRequest? body;
        try
        {
            body = await req.ReadFromJsonAsync<SponsorRequest>();
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Ongeldig verzoek." });
        }

        if (body == null
            || string.IsNullOrWhiteSpace(body.CompanyName)
            || string.IsNullOrWhiteSpace(body.ContactName)
            || string.IsNullOrWhiteSpace(body.Email)
            || string.IsNullOrWhiteSpace(body.Package))
        {
            return new BadRequestObjectResult(new { error = "Vul alle verplichte velden in." });
        }

        var requestId = Guid.NewGuid().ToString();

        try
        {
            var entity = new SponsorRequestEntity
            {
                RowKey = requestId,
                CompanyName = body.CompanyName,
                ContactName = body.ContactName,
                Email = body.Email,
                Phone = body.Phone ?? "",
                Package = body.Package,
                Message = body.Message ?? "",
            };

            await _storage.SaveSponsorRequestAsync(entity);

            // Send confirmation to the sponsor
            await _email.SendSponsorConfirmationAsync(body.Email, body.CompanyName, body.Package);

            // Notify the committee
            var contactEmail = string.IsNullOrEmpty(_appOptions.Value.ContactEmail)
                ? "oudercomitepittem@gmail.com"
                : _appOptions.Value.ContactEmail;
            await _email.SendContactNotificationAsync(
                body.ContactName,
                body.Email,
                $"Sponsoraanvraag: {body.Package} — {body.CompanyName}",
                $"Bedrijf: {body.CompanyName}\nContactpersoon: {body.ContactName}\nTelefoon: {body.Phone}\nPakket: {body.Package}\n\n{body.Message}",
                contactEmail);

            _logger.LogInformation("Sponsor request {RequestId} created for {Company}", requestId, body.CompanyName);

            return new OkObjectResult(new SponsorResponse(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process sponsor request {RequestId}", requestId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
