using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class QrCodeHelperTests
{
    [Fact]
    public void GeneratePng_ValidPayload_ReturnsNonEmptyBytes()
    {
        var result = QrCodeHelper.GeneratePng("abc123:XaBcDeFgHiJk");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GeneratePng_ValidPayload_ReturnsPngMagicBytes()
    {
        var result = QrCodeHelper.GeneratePng("test-ticket:signature");

        // PNG files start with the 8-byte signature: 0x89 0x50 0x4E 0x47 0x0D 0x0A 0x1A 0x0A
        Assert.Equal(0x89, result[0]);
        Assert.Equal(0x50, result[1]); // 'P'
        Assert.Equal(0x4E, result[2]); // 'N'
        Assert.Equal(0x47, result[3]); // 'G'
    }

    [Fact]
    public void GenerateBase64_ValidPayload_ReturnsNonEmptyString()
    {
        var result = QrCodeHelper.GenerateBase64("abc123:XaBcDeFgHiJk");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GenerateBase64_ValidPayload_ReturnsValidBase64()
    {
        var result = QrCodeHelper.GenerateBase64("abc123:XaBcDeFgHiJk");

        var exception = Record.Exception(() => Convert.FromBase64String(result));
        Assert.Null(exception);
    }

    [Fact]
    public void GenerateBase64_AndGeneratePng_ProduceSameImage()
    {
        const string payload = "ticket-id:signature12";

        var pngBytes = QrCodeHelper.GeneratePng(payload);
        var base64 = QrCodeHelper.GenerateBase64(payload, pixelsPerModule: 5);
        var base64Bytes = Convert.FromBase64String(base64);

        Assert.Equal(pngBytes, base64Bytes);
    }
}
