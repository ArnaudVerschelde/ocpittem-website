using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OCPittem.Functions;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Functions;

public class GalleryFunctionTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IOptions<StorageOptions> _storageOptions =
        Options.Create(new StorageOptions { BlobContainerGallery2026 = "fotos-2026" });
    private readonly ILogger<GalleryFunction> _logger = Substitute.For<ILogger<GalleryFunction>>();
    private readonly GalleryFunction _sut;

    public GalleryFunctionTests()
    {
        _sut = new GalleryFunction(_storage, _storageOptions, _logger);
    }

    [Fact]
    public async Task Run_ReturnsImagesFromConfiguredContainer()
    {
        var images = new List<string>
        {
            "https://example.blob.core.windows.net/fotos-2026/a.jpg?sas",
            "https://example.blob.core.windows.net/fotos-2026/b.png?sas",
        };
        _storage.GetGalleryImageUrlsAsync("fotos-2026", Arg.Any<TimeSpan>())
            .Returns(images);

        var result = await _sut.Run(HttpRequestHelper.CreateGetRequest());

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        var imagesProp = value.GetType().GetProperty("images")!.GetValue(value);
        var returned = Assert.IsAssignableFrom<IReadOnlyList<string>>(imagesProp);
        Assert.Equal(images, returned);

        await _storage.Received(1).GetGalleryImageUrlsAsync("fotos-2026", Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task Run_EmptyContainer_ReturnsOkWithEmptyList()
    {
        _storage.GetGalleryImageUrlsAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(Array.Empty<string>());

        var result = await _sut.Run(HttpRequestHelper.CreateGetRequest());

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        var imagesProp = value.GetType().GetProperty("images")!.GetValue(value);
        var returned = Assert.IsAssignableFrom<IReadOnlyList<string>>(imagesProp);
        Assert.Empty(returned);
    }

    [Fact]
    public async Task Run_StorageThrows_Returns500()
    {
        _storage.GetGalleryImageUrlsAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .ThrowsAsync(new Exception("Storage down"));

        var result = await _sut.Run(HttpRequestHelper.CreateGetRequest());

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
    }
}
