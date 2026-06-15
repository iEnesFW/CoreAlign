using CoreAlign.Application.Common.Storage;
using CoreAlign.Domain.Entities.Whitelabel;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Whitelabel;

public sealed class TenantThemeService : ITenantThemeService
{
    private readonly ITenantThemeRepository _repo;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;

    public TenantThemeService(
        ITenantThemeRepository repo,
        IFileStorage storage,
        IUnitOfWork uow)
    {
        _repo = repo;
        _storage = storage;
        _uow = uow;
    }

    public async Task<TenantThemeDto> GetThemeAsync(Guid tenantId, CancellationToken ct)
    {
        var theme = await GetOrCreateAsync(tenantId, ct);
        var logoUrl = await ResolveAssetUrlAsync(tenantId, TenantThemeAssetKind.Logo, ct);
        var faviconUrl = await ResolveAssetUrlAsync(tenantId, TenantThemeAssetKind.Favicon, ct);
        var loginBgUrl = await ResolveAssetUrlAsync(tenantId, TenantThemeAssetKind.LoginBackground, ct);
        return ToDto(theme, logoUrl, faviconUrl, loginBgUrl);
    }

    public async Task<TenantThemeDto> UpdateThemeAsync(Guid tenantId, UpdateTenantThemePayload payload, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var theme = await GetOrCreateAsync(tenantId, ct);

        if (!string.IsNullOrWhiteSpace(payload.CustomSubdomain))
        {
            var exists = await _repo.SubdomainExistsAsync(payload.CustomSubdomain, tenantId, ct);
            if (exists)
            {
                throw new InvalidOperationException("Subdomain is already in use by another tenant.");
            }
        }

        var now = DateTime.UtcNow;
        theme.UpdateColors(payload.PrimaryColor, payload.AccentColor, now);
        theme.UpdateBranding(payload.BrandName, payload.EmailFromName, payload.EmailFromAddress, now);
        theme.UpdateDomains(payload.CustomSubdomain, payload.CustomDomain, now);
        theme.UpdateLoginPage(payload.LoginHeadingMd, now);

        await _uow.SaveChangesAsync(ct);

        var logoUrl = await ResolveAssetUrlAsync(tenantId, TenantThemeAssetKind.Logo, ct);
        var faviconUrl = await ResolveAssetUrlAsync(tenantId, TenantThemeAssetKind.Favicon, ct);
        var loginBgUrl = await ResolveAssetUrlAsync(tenantId, TenantThemeAssetKind.LoginBackground, ct);
        return ToDto(theme, logoUrl, faviconUrl, loginBgUrl);
    }

    public async Task<TenantThemeAssetDto> UploadAssetAsync(
        Guid tenantId,
        TenantThemeAssetKind kind,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken ct)
    {
        if (!TenantThemeAssetPolicy.IsAllowedFor(kind, contentType))
        {
            throw new ArgumentException("Unsupported asset content type for the requested kind.", nameof(contentType));
        }

        if (sizeBytes <= 0 || sizeBytes > TenantThemeAssetPolicy.MaxBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), $"Asset must be between 1 and {TenantThemeAssetPolicy.MaxBytes} bytes.");
        }

        var theme = await GetOrCreateAsync(tenantId, ct);

        var safeName = SanitizeUploadFileName(fileName, kind);

        var stored = await _storage.SaveAsync(
            $"{TenantThemeAssetPolicy.StorageScope}/{tenantId:N}/{kind.ToString().ToLowerInvariant()}",
            safeName,
            content,
            contentType,
            ct);

        var asset = new TenantThemeAsset(
            tenantId,
            kind,
            Guid.NewGuid(),
            stored.ContentType,
            stored.SizeBytes,
            stored.PublicUrl);

        await _repo.AddAssetAsync(asset, ct);
        theme.SetAssetFileId(kind, asset.FileId, DateTime.UtcNow);

        await _uow.SaveChangesAsync(ct);

        return new TenantThemeAssetDto(
            asset.Id,
            asset.AssetKind,
            asset.ContentType,
            asset.SizeBytes,
            asset.PublicUrl,
            asset.CreatedAtUtc);
    }

    public async Task<PublicTenantThemeDto?> GetPublicThemeBySubdomainAsync(string subdomain, CancellationToken ct)
    {
        var theme = await _repo.GetBySubdomainAsync(subdomain, ct);
        if (theme is null) return null;
        return await ToPublicDtoAsync(theme, ct);
    }

    public async Task<PublicTenantThemeDto?> GetPublicThemeByCustomDomainAsync(string domain, CancellationToken ct)
    {
        var theme = await _repo.GetByCustomDomainAsync(domain, ct);
        if (theme is null) return null;
        return await ToPublicDtoAsync(theme, ct);
    }

    private static string SanitizeUploadFileName(string? fileName, TenantThemeAssetKind kind)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return $"{kind}-{Guid.NewGuid():N}";
        }

        var leaf = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(leaf))
        {
            return $"{kind}-{Guid.NewGuid():N}";
        }

        var invalid = Path.GetInvalidFileNameChars();
        Span<char> buffer = stackalloc char[leaf.Length];
        var index = 0;
        foreach (var ch in leaf)
        {
            if (ch == '/' || ch == '\\' || ch == ':' || Array.IndexOf(invalid, ch) >= 0)
            {
                continue;
            }
            buffer[index++] = ch;
        }

        var sanitized = new string(buffer[..index]).Trim().TrimStart('.');
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length > 128)
        {
            return $"{kind}-{Guid.NewGuid():N}";
        }
        return sanitized;
    }

    private async Task<TenantTheme> GetOrCreateAsync(Guid tenantId, CancellationToken ct)
    {
        var existing = await _repo.GetByTenantIdAsync(tenantId, ct);
        if (existing is not null) return existing;
        var theme = new TenantTheme(tenantId);
        await _repo.AddAsync(theme, ct);
        await _uow.SaveChangesAsync(ct);
        return theme;
    }

    private async Task<string?> ResolveAssetUrlAsync(Guid tenantId, TenantThemeAssetKind kind, CancellationToken ct)
    {
        var asset = await _repo.GetLatestAssetAsync(tenantId, kind, ct);
        return asset?.PublicUrl;
    }

    private static TenantThemeDto ToDto(TenantTheme theme, string? logoUrl, string? faviconUrl, string? loginBgUrl) =>
        new(
            theme.TenantId,
            theme.PrimaryColor,
            theme.AccentColor,
            theme.BrandName,
            theme.CustomSubdomain,
            theme.CustomDomain,
            theme.EmailFromName,
            theme.EmailFromAddress,
            theme.LoginHeadingMd,
            logoUrl,
            faviconUrl,
            loginBgUrl,
            theme.ConcurrencyToken);

    private async Task<PublicTenantThemeDto> ToPublicDtoAsync(TenantTheme theme, CancellationToken ct)
    {
        var logoUrl = await ResolveAssetUrlAsync(theme.TenantId, TenantThemeAssetKind.Logo, ct);
        var faviconUrl = await ResolveAssetUrlAsync(theme.TenantId, TenantThemeAssetKind.Favicon, ct);
        var loginBgUrl = await ResolveAssetUrlAsync(theme.TenantId, TenantThemeAssetKind.LoginBackground, ct);
        return new PublicTenantThemeDto(
            theme.TenantId,
            theme.PrimaryColor,
            theme.AccentColor,
            theme.BrandName,
            logoUrl,
            faviconUrl,
            loginBgUrl,
            theme.LoginHeadingMd);
    }
}
