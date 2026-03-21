using QRCoder;

namespace OCPittem.Functions.Services;

internal static class QrCodeHelper
{
    internal static byte[] GeneratePng(string payload, int pixelsPerModule = 5)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(pixelsPerModule);
    }

    internal static string GenerateBase64(string payload, int pixelsPerModule = 4)
        => Convert.ToBase64String(GeneratePng(payload, pixelsPerModule));
}
