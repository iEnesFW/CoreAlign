using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Whitelabel;

public class TenantThemeAsset : TenantEntity
{
    public TenantThemeAssetKind AssetKind { get; private set; }
    public Guid FileId { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? PublicUrl { get; private set; }

    protected TenantThemeAsset() { }

    public TenantThemeAsset(
        Guid tenantId,
        TenantThemeAssetKind assetKind,
        Guid fileId,
        string contentType,
        long sizeBytes,
        string? publicUrl)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (fileId == Guid.Empty) throw new ArgumentException("FileId is required.", nameof(fileId));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("ContentType is required.", nameof(contentType));
        if (sizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes), "SizeBytes must be positive.");

        TenantId = tenantId;
        AssetKind = assetKind;
        FileId = fileId;
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        PublicUrl = publicUrl;
    }
}
