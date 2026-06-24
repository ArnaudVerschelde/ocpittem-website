namespace OCPittem.Functions.Services;

public static class GalleryImageFilter
{
    private static readonly string[] SupportedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".avif"];

    public static bool IsSupportedImage(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return false;

        var ext = Path.GetExtension(blobName);
        return !string.IsNullOrEmpty(ext)
            && SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
}
