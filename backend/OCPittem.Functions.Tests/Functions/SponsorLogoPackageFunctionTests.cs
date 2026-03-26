using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OCPittem.Functions.Functions;
using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Functions;

public class SponsorLogoPackageFunctionTests
{
    private readonly ISponsorLogoPackageService _service = Substitute.For<ISponsorLogoPackageService>();
    private readonly ILogger<SponsorLogoPackageFunction> _logger = Substitute.For<ILogger<SponsorLogoPackageFunction>>();
    private readonly SponsorLogoPackageFunction _sut;

    public SponsorLogoPackageFunctionTests()
    {
        _sut = new SponsorLogoPackageFunction(_service, _logger);
    }

    [Fact]
    public async Task Run_ReturnsZipFile_WithCorrectContentType()
    {
        var zipBytes = new byte[] { 0x50, 0x4B, 0x05, 0x06 };
        _service.CreateLogosZipAsync().Returns(zipBytes);

        var result = await _sut.Run(new DefaultHttpContext().Request);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", fileResult.ContentType);
        Assert.Equal(zipBytes, fileResult.FileContents);
        Assert.StartsWith("sponsor-logos-", fileResult.FileDownloadName);
        Assert.EndsWith(".zip", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task Run_ServiceThrows_Returns500()
    {
        _service.CreateLogosZipAsync().Throws(new Exception("Storage error"));

        var result = await _sut.Run(new DefaultHttpContext().Request);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
