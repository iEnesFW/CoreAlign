namespace CoreAlign.Application.Products.Images;

public static class ProductImagePolicy
{
    public const long MaxBytesPerImage = 5L * 1024L * 1024L;
    public const int MaxImagesPerProduct = 10;
    public const string StorageScope = "product-images";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
    };

    private static readonly Dictionary<string, HashSet<string>> ExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" },
        ["image/jpg"] = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" },
        ["image/png"] = new(StringComparer.OrdinalIgnoreCase) { ".png" },
        ["image/webp"] = new(StringComparer.OrdinalIgnoreCase) { ".webp" },
    };

    public static bool IsAllowedContentType(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType);

    public static bool IsAllowedExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    public static bool MatchesContentTypeAndExtension(string? contentType, string? fileName)
    {
        if (!IsAllowedContentType(contentType) || !IsAllowedExtension(fileName)) return false;
        var ext = Path.GetExtension(fileName!);
        return ExtensionsByContentType.TryGetValue(contentType!, out var allowed) && allowed.Contains(ext);
    }

    public static async Task<bool> LooksLikeImageAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (content is null) return false;
        if (!content.CanSeek) return true;

        var origin = content.Position;
        try
        {
            content.Position = 0;
            var header = new byte[12];
            var read = await content.ReadAsync(header.AsMemory(0, 12), cancellationToken);
            if (read < 4) return false;
            return IsJpeg(header, read)
                || IsPng(header, read)
                || IsWebp(header, read);
        }
        finally
        {
            content.Position = origin;
        }
    }

    private static bool IsJpeg(byte[] h, int len)
        => len >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;

    private static bool IsPng(byte[] h, int len)
        => len >= 8
           && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47
           && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A;

    private static bool IsWebp(byte[] h, int len)
        => len >= 12
           && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
           && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;
}
