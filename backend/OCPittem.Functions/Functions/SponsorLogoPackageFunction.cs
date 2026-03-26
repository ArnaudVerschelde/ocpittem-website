using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class SponsorLogoPackageFunction
{
    private readonly ISponsorLogoPackageService _service;
    private readonly ILogger<SponsorLogoPackageFunction> _logger;

    public SponsorLogoPackageFunction(ISponsorLogoPackageService service, ILogger<SponsorLogoPackageFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function("SponsorLogoPackage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "sponsors/logos-zip")] HttpRequest req)
    {
        _logger.LogInformation("SponsorLogoPackage triggered.");
        try
        {
            var zipBytes = await _service.CreateLogosZipAsync();
            return new FileContentResult(zipBytes, "application/zip")
            {
                FileDownloadName = $"sponsor-logos-{DateTime.UtcNow:yyyyMMdd}.zip"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sponsor logos ZIP.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
