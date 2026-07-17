using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Functions;

public class GalleryFunction
{
    private static readonly TimeSpan SasLifetime = TimeSpan.FromHours(1);

    private readonly IStorageService _storage;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<GalleryFunction> _logger;

    public GalleryFunction(
        IStorageService storage,
        IOptions<StorageOptions> storageOptions,
        ILogger<GalleryFunction> logger)
    {
        _storage = storage;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    [Function("GalleryBalParental2026")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "gallery/bal-parental-2026")]
        HttpRequest req)
    {
        try
        {
            var images = await _storage.GetGalleryImagesAsync(
                _storageOptions.BlobContainerGallery2026,
                SasLifetime);

            return new OkObjectResult(new { images });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kon sfeerbeelden niet ophalen.");

            return new ObjectResult(
                new { error = "Kon de sfeerbeelden niet ophalen." })
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
        }
    }
}