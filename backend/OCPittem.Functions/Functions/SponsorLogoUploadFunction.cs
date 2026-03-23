using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class SponsorLogoUploadFunction
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"];

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    private readonly IStorageService _storage;
    private readonly ILogger<SponsorLogoUploadFunction> _logger;

    public SponsorLogoUploadFunction(IStorageService storage, ILogger<SponsorLogoUploadFunction> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    [Function("SponsorLogoUpload")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sponsors/upload-logo")] HttpRequest req)
    {
        if (!req.HasFormContentType)
            return new BadRequestObjectResult(new { error = "Multipart form data verwacht." });

        IFormFile? file;
        try
        {
            var form = await req.ReadFormAsync();
            file = form.Files.GetFile("logo");
        }
        catch
        {
            return new BadRequestObjectResult(new { error = "Ongeldig verzoek." });
        }

        if (file == null || file.Length == 0)
            return new BadRequestObjectResult(new { error = "Geen bestand ontvangen." });

        if (file.Length > MaxFileSize)
            return new BadRequestObjectResult(new { error = "Bestand te groot. Maximum 5 MB toegestaan." });

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedContentTypes.Contains(contentType))
            return new BadRequestObjectResult(new { error = "Ongeldig bestandstype. Enkel afbeeldingen toegestaan (JPEG, PNG, GIF, WebP, SVG)." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            extension = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/svg+xml" => ".svg",
                _ => ".bin",
            };

        var logoId = Guid.NewGuid().ToString();
        try
        {
            using var stream = file.OpenReadStream();
            var url = await _storage.SaveSponsorLogoAsync(logoId, stream, file.ContentType, extension);
            _logger.LogInformation("Sponsor logo uploaded: {LogoId}{Extension}", logoId, extension);
            return new OkObjectResult(new { logoUrl = url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload sponsor logo {LogoId}", logoId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
