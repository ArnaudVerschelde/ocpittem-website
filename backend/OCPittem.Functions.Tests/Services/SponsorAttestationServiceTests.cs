using System.Net;
using NSubstitute;
using OCPittem.Functions.Services;
using OCPittem.Functions.Tests.Helpers;

namespace OCPittem.Functions.Tests.Services;

public class SponsorAttestationServiceTests
{
    // A real PNG image (generated via QrCodeHelper) so QuestPDF can decode it
    private static readonly byte[] FakeSignatureBytes = QrCodeHelper.GeneratePng("signature-placeholder", pixelsPerModule: 2);

    private static ISponsorAttestationService CreateSut(HttpMessageHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(handler));
        return new SponsorAttestationService(factory);
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
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(FakeSignatureBytes)
            });

        var sut = CreateSut(handler);
        var result = await GenerateSample(sut);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateAttestationAsync_SignatureDownloadFails_StillReturnsPdf()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var sut = CreateSut(handler);
        var result = await GenerateSample(sut);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateAttestationAsync_HttpClientThrows_StillReturnsPdf()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("Network error"));

        var sut = CreateSut(handler);
        var result = await GenerateSample(sut);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateAttestationAsync_ReturnsPdfMagicBytes()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(FakeSignatureBytes)
            });

        var sut = CreateSut(handler);
        var result = await GenerateSample(sut);

        Assert.Equal((byte)'%', result[0]);
        Assert.Equal((byte)'P', result[1]);
        Assert.Equal((byte)'D', result[2]);
        Assert.Equal((byte)'F', result[3]);
    }
}
