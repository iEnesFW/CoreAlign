namespace CoreAlign.Application.Tenants.Logo;

public static class TenantLogoPolicy
{
    public const long MaxBytes = 1024L * 1024L;
    public const string StorageScope = "tenant-logos";

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/svg+xml",
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".svg",
    };

    private static readonly Dictionary<string, HashSet<string>> ExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" },
        ["image/jpg"] = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" },
        ["image/png"] = new(StringComparer.OrdinalIgnoreCase) { ".png" },
        ["image/svg+xml"] = new(StringComparer.OrdinalIgnoreCase) { ".svg" },
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

    public static async Task<bool> LooksLikeLogoAsync(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (content is null) return false;
        if (!content.CanSeek) return true;

        var origin = content.Position;
        try
        {
            content.Position = 0;
            var header = new byte[256];
            var read = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (read < 4) return false;
            if (string.Equals(contentType, "image/svg+xml", StringComparison.OrdinalIgnoreCase))
            {
                return IsSvg(header, read);
            }
            return IsJpeg(header, read) || IsPng(header, read);
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

    private static bool IsSvg(byte[] h, int len)
    {
        if (len < 5) return false;
        var text = System.Text.Encoding.UTF8.GetString(h, 0, Math.Min(len, h.Length)).TrimStart('﻿', ' ', '\t', '\r', '\n');
        return text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
    }
}
