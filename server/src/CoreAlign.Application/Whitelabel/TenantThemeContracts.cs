using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Whitelabel;

public sealed record TenantThemeDto(
    Guid TenantId,
    string PrimaryColor,
    string AccentColor,
    string? BrandName,
    string? CustomSubdomain,
    string? CustomDomain,
    string EmailFromName,
    string? EmailFromAddress,
    string? LoginHeadingMd,
    string? LogoUrl,
    string? FaviconUrl,
    string? LoginBackgroundUrl,
    long ConcurrencyToken);

public sealed record PublicTenantThemeDto(
    Guid TenantId,
    string PrimaryColor,
    string AccentColor,
    string? BrandName,
    string? LogoUrl,
    string? FaviconUrl,
    string? LoginBackgroundUrl,
    string? LoginHeadingMd);

public sealed record UpdateTenantThemePayload(
    string PrimaryColor,
    string AccentColor,
    string? BrandName,
    string? CustomSubdomain,
    string? CustomDomain,
    string EmailFromName,
    string? EmailFromAddress,
    string? LoginHeadingMd);

public sealed record TenantThemeAssetDto(
    Guid Id,
    TenantThemeAssetKind Kind,
    string ContentType,
    long SizeBytes,
    string? PublicUrl,
    DateTime CreatedAtUtc);
