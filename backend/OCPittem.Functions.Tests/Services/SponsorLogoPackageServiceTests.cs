using System.IO.Compression;
using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OCPittem.Functions.Models;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Services;

public class SponsorLogoPackageServiceTests
{
    private static readonly byte[] FakeLogoBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A]; // PNG header

    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<SponsorLogoPackageService> _logger = Substitute.For<ILogger<SponsorLogoPackageService>>();

    private ISponsorLogoPackageService CreateSut(HttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        return new SponsorLogoPackageService(_storage, factory, _logger);
    }

    private static ZipArchive OpenZip(byte[] bytes) =>
        new(new MemoryStream(bytes), ZipArchiveMode.Read);

    [Fact]
    public async Task CreateLogosZipAsync_NoSponsors_ReturnsEmptyZip()
    {
        _storage.GetAllSponsorRequestsAsync().Returns(new List<SponsorRequestEntity>());
        var sut = CreateSut(new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await sut.CreateLogosZipAsync();

        using var archive = OpenZip(result);
        Assert.Empty(archive.Entries);
    }

    [Fact]
    public async Task CreateLogosZipAsync_SponsorsWithLogos_ZipContainsCorrectEntries()
    {
        var sponsors = new List<SponsorRequestEntity>
        {
            new() { CompanyName = "Bakkerij Janssen", LogoUrl = "https://storage.test/logos/abc123.png" },
            new() { CompanyName = "Slagerij Peeters", LogoUrl = "https://storage.test/logos/def456.jpg" },
        };
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);
        var sut = CreateSut(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakeLogoBytes) }));

        var result = await sut.CreateLogosZipAsync();

        using var archive = OpenZip(result);
        Assert.Equal(2, archive.Entries.Count);
        Assert.Contains(archive.Entries, e => e.FullName == "Bakkerij Janssen/Bakkerij Janssen.png");
        Assert.Contains(archive.Entries, e => e.FullName == "Slagerij Peeters/Slagerij Peeters.jpg");
    }

    [Fact]
    public async Task CreateLogosZipAsync_SponsorWithoutLogo_IsSkipped()
    {
        var sponsors = new List<SponsorRequestEntity>
        {
            new() { CompanyName = "Bakkerij Janssen", LogoUrl = "https://storage.test/logos/abc123.png" },
            new() { CompanyName = "Geen Logo NV", LogoUrl = "" },
        };
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);
        var sut = CreateSut(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakeLogoBytes) }));

        var result = await sut.CreateLogosZipAsync();

        using var archive = OpenZip(result);
        Assert.Single(archive.Entries);
        Assert.Equal("Bakkerij Janssen/Bakkerij Janssen.png", archive.Entries[0].FullName);
    }

    [Fact]
    public async Task CreateLogosZipAsync_LogoDownloadFails_SponsorSkipped()
    {
        var sponsors = new List<SponsorRequestEntity>
        {
            new() { CompanyName = "Bakkerij Janssen", LogoUrl = "https://storage.test/logos/abc123.png" },
            new() { CompanyName = "Slagerij Peeters", LogoUrl = "https://storage.test/logos/def456.jpg" },
        };
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);

        var callCount = 0;
        var sut = CreateSut(new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            return callCount == 1
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakeLogoBytes) };
        }));

        var result = await sut.CreateLogosZipAsync();

        using var archive = OpenZip(result);
        Assert.Single(archive.Entries);
        Assert.Equal("Slagerij Peeters/Slagerij Peeters.jpg", archive.Entries[0].FullName);
    }

    [Fact]
    public async Task CreateLogosZipAsync_HttpClientThrows_SponsorSkipped()
    {
        var sponsors = new List<SponsorRequestEntity>
        {
            new() { CompanyName = "Bakkerij Janssen", LogoUrl = "https://storage.test/logos/abc123.png" },
        };
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);
        var sut = CreateSut(new FakeHttpMessageHandler(_ => throw new HttpRequestException("Network error")));

        var result = await sut.CreateLogosZipAsync();

        using var archive = OpenZip(result);
        Assert.Empty(archive.Entries);
    }

    [Fact]
    public async Task CreateLogosZipAsync_DuplicateCompanyNames_AddsSuffix()
    {
        var sponsors = new List<SponsorRequestEntity>
        {
            new() { CompanyName = "Bedrijf A", LogoUrl = "https://storage.test/logos/logo1.png" },
            new() { CompanyName = "Bedrijf A", LogoUrl = "https://storage.test/logos/logo2.png" },
        };
        _storage.GetAllSponsorRequestsAsync().Returns(sponsors);
        var sut = CreateSut(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakeLogoBytes) }));

        var result = await sut.CreateLogosZipAsync();

        using var archive = OpenZip(result);
        Assert.Equal(2, archive.Entries.Count);
        Assert.Contains(archive.Entries, e => e.FullName == "Bedrijf A/Bedrijf A.png");
        Assert.Contains(archive.Entries, e => e.FullName == "Bedrijf A (2)/Bedrijf A (2).png");
    }

    [Theory]
    [InlineData("https://account.blob.core.windows.net/logos/abc.png?sv=2022&sig=xxx", ".png")]
    [InlineData("https://account.blob.core.windows.net/logos/abc.jpg?sv=2022&sig=xxx", ".jpg")]
    [InlineData("https://account.blob.core.windows.net/logos/abc.svg?sv=2022", ".svg")]
    [InlineData("not-a-url", ".bin")]
    public void GetExtensionFromUrl_ReturnsCorrectExtension(string url, string expected) =>
        Assert.Equal(expected, SponsorLogoPackageService.GetExtensionFromUrl(url));

    [Theory]
    [InlineData("Bakkerij Janssen NV", "Bakkerij Janssen NV")]
    [InlineData("  Trimmed  ", "Trimmed")]
    [InlineData("", "Onbekend")]
    public void AllocateFolderName_SanitizesCorrectly(string input, string expected)
    {
        var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expected, SponsorLogoPackageService.AllocateFolderName(input, usedNames));
    }
}
