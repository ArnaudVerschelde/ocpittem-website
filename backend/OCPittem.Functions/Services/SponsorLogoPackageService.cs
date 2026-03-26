using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace OCPittem.Functions.Services;

public class SponsorLogoPackageService : ISponsorLogoPackageService
{
    private readonly IStorageService _storage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SponsorLogoPackageService> _logger;

    public SponsorLogoPackageService(
        IStorageService storage,
        IHttpClientFactory httpClientFactory,
        ILogger<SponsorLogoPackageService> logger)
    {
        _storage = storage;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<byte[]> CreateLogosZipAsync()
    {
        var sponsors = await _storage.GetAllSponsorRequestsAsync();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var http = _httpClientFactory.CreateClient();
            var usedFolderNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var sponsor in sponsors.Where(s => !string.IsNullOrEmpty(s.LogoUrl)))
            {
                var logoBytes = await TryDownloadLogoAsync(http, sponsor.LogoUrl, sponsor.CompanyName);
                if (logoBytes is null) continue;

                var extension = GetExtensionFromUrl(sponsor.LogoUrl);
                var folderName = AllocateFolderName(sponsor.CompanyName, usedFolderNames);
                var entryName = $"{folderName}/{folderName}{extension}";

                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(logoBytes);
            }
        }

        return memoryStream.ToArray();
    }

    private async Task<byte[]?> TryDownloadLogoAsync(HttpClient http, string logoUrl, string companyName)
    {
        try
        {
            var response = await http.GetAsync(logoUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Logo download returned {StatusCode} for {Company}.",
                    (int)response.StatusCode, companyName);
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download logo for {Company}.", companyName);
            return null;
        }
    }

    internal static string GetExtensionFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return ".bin";
        var ext = Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrEmpty(ext) ? ".bin" : ext.ToLowerInvariant();
    }

    internal static string AllocateFolderName(string companyName, Dictionary<string, int> usedNames)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(companyName.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "Onbekend";

        if (!usedNames.TryGetValue(sanitized, out var count))
        {
            usedNames[sanitized] = 1;
            return sanitized;
        }

        usedNames[sanitized] = count + 1;
        return $"{sanitized} ({count + 1})";
    }
}
