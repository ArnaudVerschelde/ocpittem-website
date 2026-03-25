using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OCPittem.Functions;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Services;

public class SponsorAttestationServiceTests
{
    private static readonly byte[] FakeSignatureBytes = QrCodeHelper.GeneratePng("signature-placeholder", pixelsPerModule: 2);

    private static ISponsorAttestationService CreateSut(BlobServiceClient blobServiceClient)
    {
        var options = Options.Create(new SponsorAttestationOptions());
        var logger = Substitute.For<ILogger<SponsorAttestationService>>();
        return new SponsorAttestationService(blobServiceClient, options, logger);
    }

    private static BlobServiceClient BlobServiceWithSignature()
    {
        var blobClient = Substitute.For<BlobClient>();

        var existsResponse = Substitute.For<Response<bool>>();
        existsResponse.Value.Returns(true);
        blobClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(existsResponse);

        var content = BinaryData.FromBytes(FakeSignatureBytes);
        var downloadResult = BlobsModelFactory.BlobDownloadResult(content: content);
        var downloadResponse = Substitute.For<Response<BlobDownloadResult>>();
        downloadResponse.Value.Returns(downloadResult);
        blobClient.DownloadContentAsync(Arg.Any<CancellationToken>()).Returns(downloadResponse);

        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);

        var serviceClient = Substitute.For<BlobServiceClient>();
        serviceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(containerClient);
        return serviceClient;
    }

    private static BlobServiceClient BlobServiceBlobNotFound()
    {
        var blobClient = Substitute.For<BlobClient>();
        var existsResponse = Substitute.For<Response<bool>>();
        existsResponse.Value.Returns(false);
        blobClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(existsResponse);

        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);

        var serviceClient = Substitute.For<BlobServiceClient>();
        serviceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(containerClient);
        return serviceClient;
    }

    private static BlobServiceClient BlobServiceThatThrows()
    {
        var blobClient = Substitute.For<BlobClient>();
        blobClient.ExistsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Response<bool>>(new RequestFailedException("Network error")));

        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);

        var serviceClient = Substitute.For<BlobServiceClient>();
        serviceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(containerClient);
        return serviceClient;
    }

    private static Task<byte[]> GenerateSample(ISponsorAttestationService sut) =>
        sut.GenerateAttestationAsync(
            companyName: "Bakkerij Janssen NV",
            street: "Marktstraat",
            houseNumber: "12",
            postalCode: "8740",
            city: "Pittem",
            enterpriseNumber: "0123.456.789",
            amount: 250.00m,
            date: new DateTime(2026, 6, 20));

    [Fact]
    public async Task GenerateAttestationAsync_SignatureAvailable_ReturnsNonEmptyPdf()
    {
        var sut = CreateSut(BlobServiceWithSignature());
        var result = await GenerateSample(sut);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateAttestationAsync_SignatureDownloadFails_StillReturnsPdf()
    {
        var sut = CreateSut(BlobServiceBlobNotFound());
        var result = await GenerateSample(sut);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateAttestationAsync_BlobClientThrows_StillReturnsPdf()
    {
        var sut = CreateSut(BlobServiceThatThrows());
        var result = await GenerateSample(sut);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateAttestationAsync_ReturnsPdfMagicBytes()
    {
        var sut = CreateSut(BlobServiceWithSignature());
        var result = await GenerateSample(sut);

        Assert.Equal((byte)'%', result[0]);
        Assert.Equal((byte)'P', result[1]);
        Assert.Equal((byte)'D', result[2]);
        Assert.Equal((byte)'F', result[3]);
    }
}
