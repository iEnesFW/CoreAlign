using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Whitelabel;

public static class TenantThemeAssetPolicy
{
    public const long MaxBytes = 2L * 1024L * 1024L;
    public const string StorageScope = "tenant-theme";

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/svg+xml",
        "image/x-icon",
        "image/vnd.microsoft.icon",
        "image/webp",
    };

    public static bool IsAllowedFor(TenantThemeAssetKind kind, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        return kind switch
        {
            TenantThemeAssetKind.Logo => AllowedImageContentTypes.Contains(contentType),
            TenantThemeAssetKind.Favicon => AllowedImageContentTypes.Contains(contentType),
            TenantThemeAssetKind.LoginBackground => AllowedImageContentTypes.Contains(contentType),
            TenantThemeAssetKind.EmailHeader => AllowedImageContentTypes.Contains(contentType),
            _ => false,
        };
    }
}
