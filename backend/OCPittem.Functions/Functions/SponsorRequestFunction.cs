using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class SponsorRequestFunction
{
    private readonly IStripeService _stripe;
    private readonly IStorageService _storage;
    private readonly ILogger<SponsorRequestFunction> _logger;

    public SponsorRequestFunction(IStripeService stripe, IStorageService storage, ILogger<SponsorRequestFunction> logger)
    {
        _stripe = stripe;
        _storage = storage;
        _logger = logger;
    }

    [Function("SponsorCheckout")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sponsors/checkout")] HttpRequest req)
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

        if (body.ExtraEtenPartyCount < 0 || body.ExtraVegetarischCount < 0 || body.ExtraDrankkaart20Count < 0 || body.IncludedVegetarischCount < 0)
            return new BadRequestObjectResult(new { error = "Ongeldige aantallen." });

        if (body.ExtraVegetarischCount > body.ExtraEtenPartyCount)
            return new BadRequestObjectResult(new { error = "Aantal vegetarische opties mag niet groter zijn dan het aantal extra tickets." });

        var includedTicketCount = body.Package.ToLower() switch { "zilver" => 2, "goud" => 4, _ => 0 };
        if (body.IncludedVegetarischCount > includedTicketCount)
            return new BadRequestObjectResult(new { error = "Aantal vegetarische opties mag niet groter zijn dan het aantal inbegrepen tickets." });

        var validPackages = new[] { "brons", "zilver", "goud" };
        if (!validPackages.Contains(body.Package.ToLower()))
            return new BadRequestObjectResult(new { error = "Ongeldig sponsorpakket." });

        var requestId = Guid.NewGuid().ToString();

        try
        {
            var checkout = await _stripe.CreateSponsorCheckoutSessionAsync(
                requestId, body.Email, body.CompanyName,
                body.Package, body.ExtraEtenPartyCount, body.ExtraDrankkaart20Count);

            var entity = new SponsorRequestEntity
            {
                PartitionKey = "Sponsor",
                RowKey = requestId,
                StripeSessionId = checkout.SessionId,
                Status = "Pending",
                CompanyName = body.CompanyName,
                ContactName = body.ContactName,
                Email = body.Email,
                Phone = body.Phone ?? "",
                Package = body.Package,
                Message = body.Message ?? "",
                ExtraEtenPartyCount = body.ExtraEtenPartyCount,
                ExtraVegetarischCount = body.ExtraVegetarischCount,
                ExtraDrankkaart20Count = body.ExtraDrankkaart20Count,
                IncludedVegetarischCount = body.IncludedVegetarischCount,
            };

            await _storage.SaveSponsorRequestAsync(entity);

            _logger.LogInformation("Sponsor checkout created for request {RequestId} ({Company}, {Package})",
                requestId, body.CompanyName, body.Package);

            return new OkObjectResult(new SponsorCheckoutResponse(checkout.Url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sponsor checkout for request {RequestId}", requestId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
