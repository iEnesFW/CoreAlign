using System;
using System.Collections.Generic;

namespace CoreAlign.Application.Common.Upload;

public enum FileUploadContentKind
{
    Binary,
    Data,
}

public sealed record FileUploadProfile(
    string Name,
    IReadOnlySet<string> AllowedContentTypes,
    IReadOnlySet<string> AllowedExtensions,
    long MaxBytes,
    bool AllowSvg,
    FileUploadContentKind ContentKind = FileUploadContentKind.Binary);

public static class FileUploadProfiles
{
    private const long Mb = 1024L * 1024L;

    public static readonly FileUploadProfile Image = new(
        "image",
        Set("image/jpeg", "image/png", "image/webp", "image/gif"),
        Set(".jpg", ".jpeg", ".png", ".webp", ".gif"),
        5 * Mb,
        AllowSvg: false);

    public static readonly FileUploadProfile Logo = new(
        "logo",
        Set("image/png", "image/jpeg", "image/svg+xml"),
        Set(".png", ".jpg", ".jpeg", ".svg"),
        1 * Mb,
        AllowSvg: true);

    public static readonly FileUploadProfile Document = new(
        "document",
        Set("application/pdf", "image/jpeg", "image/png"),
        Set(".pdf", ".jpg", ".jpeg", ".png"),
        10 * Mb,
        AllowSvg: false);

    public static readonly FileUploadProfile Attachment = new(
        "attachment",
        Set("image/jpeg", "image/png", "image/webp", "application/pdf"),
        Set(".jpg", ".jpeg", ".png", ".webp", ".pdf"),
        5 * Mb,
        AllowSvg: false);

    public static readonly FileUploadProfile ProductImage = new(
        "product-image",
        Set("image/jpeg", "image/jpg", "image/png", "image/webp"),
        Set(".jpg", ".jpeg", ".png", ".webp"),
        5 * Mb,
        AllowSvg: false);

    public static readonly FileUploadProfile TenantLogo = new(
        "tenant-logo",
        Set("image/jpeg", "image/jpg", "image/png", "image/svg+xml"),
        Set(".jpg", ".jpeg", ".png", ".svg"),
        1 * Mb,
        AllowSvg: true);

    public static readonly FileUploadProfile TenantTheme = new(
        "tenant-theme",
        Set(
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/svg+xml",
            "image/webp",
            "image/x-icon",
            "image/vnd.microsoft.icon"),
        Set(".jpg", ".jpeg", ".png", ".svg", ".webp", ".ico"),
        2 * Mb,
        AllowSvg: true);

    public static readonly FileUploadProfile GlassPhoto = new(
        "glass-photo",
        Set("image/jpeg", "image/png", "image/webp", "image/heic", "image/heif"),
        Set(".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif"),
        25 * Mb,
        AllowSvg: false);

    public static readonly FileUploadProfile Import = new(
        "import",
        Set(
            "text/csv",
            "application/csv",
            "text/plain",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/octet-stream"),
        Set(".csv", ".xlsx"),
        10 * Mb,
        AllowSvg: false,
        ContentKind: FileUploadContentKind.Data);

    private static readonly IReadOnlyDictionary<string, FileUploadProfile> ByName =
        new Dictionary<string, FileUploadProfile>(StringComparer.OrdinalIgnoreCase)
        {
            [Image.Name] = Image,
            [Logo.Name] = Logo,
            [Document.Name] = Document,
            [Attachment.Name] = Attachment,
            [ProductImage.Name] = ProductImage,
            [TenantLogo.Name] = TenantLogo,
            [TenantTheme.Name] = TenantTheme,
            [GlassPhoto.Name] = GlassPhoto,
            [Import.Name] = Import,
        };

    public static FileUploadProfile Resolve(string name) =>
        ByName.TryGetValue(name, out var profile)
            ? profile
            : throw new ArgumentException($"Unknown upload profile '{name}'.", nameof(name));

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
