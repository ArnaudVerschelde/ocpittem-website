using OCPittem.Functions.Services;

namespace OCPittem.Functions.Tests.Services;

public class GalleryImageFilterTests
{
    [Theory]
    [InlineData("foto.jpg")]
    [InlineData("foto.jpeg")]
    [InlineData("foto.png")]
    [InlineData("foto.webp")]
    [InlineData("foto.avif")]
    [InlineData("FOTO.JPG")]
    [InlineData("Foto.JpEg")]
    [InlineData("map/submap/foto.png")]
    [InlineData("naam met spaties.webp")]
    [InlineData("foto.met.punten.jpg")]
    public void IsSupportedImage_SupportedExtension_ReturnsTrue(string blobName)
    {
        Assert.True(GalleryImageFilter.IsSupportedImage(blobName));
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("video.mp4")]
    [InlineData("archief.zip")]
    [InlineData("tekst.txt")]
    [InlineData("afbeelding.gif")]
    [InlineData("afbeelding.bmp")]
    [InlineData("afbeelding.svg")]
    [InlineData("zonderextensie")]
    [InlineData("map/")]
    [InlineData("foto.jpg.txt")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSupportedImage_UnsupportedOrInvalid_ReturnsFalse(string blobName)
    {
        Assert.False(GalleryImageFilter.IsSupportedImage(blobName));
    }
}
