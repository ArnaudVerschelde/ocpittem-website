namespace OCPittem.Functions.Models;

public sealed record GalleryImageDto(
    string Name,
    string Category,
    string OriginalUrl,
    string ThumbnailUrl);