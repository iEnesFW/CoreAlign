using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;

public record MarketplaceTemplateSummaryDto(
    Guid Id,
    string Code,
    string DisplayNameKey,
    EnclosureCategory Category,
    EnclosureSubtype Subtype,
    string? ThumbnailUrl,
    string? DescriptionKey,
    Guid? SubmittedByTenantId,
    DateTime? PublishedAtUtc,
    int DownloadCount,
    decimal? AverageRating,
    int ReviewCount);

public record MarketplaceTemplateDetailDto(
    Guid Id,
    string Code,
    string DisplayNameKey,
    EnclosureCategory Category,
    EnclosureSubtype Subtype,
    GeometryMode GeometryMode,
    MountingTopology MountingTopology,
    ConnectorKind DefaultConnectorKind,
    decimal? RoofPitchDeg,
    int? RidgeHeightMm,
    int? EaveHeightMm,
    string? ThumbnailUrl,
    string? DescriptionKey,
    string? MetadataJson,
    ProjectTemplateVisibility Visibility,
    Guid? SubmittedByTenantId,
    DateTime? SubmittedAtUtc,
    DateTime? PublishedAtUtc,
    int DownloadCount,
    decimal? AverageRating,
    int ReviewCount,
    string? RejectionReason,
    int RunPresetCount);

public record MarketplaceListRequestDto(
    EnclosureCategory? Category,
    MarketplaceSortBy SortBy,
    int Skip,
    int Take);

public record MarketplaceSubmissionDto(
    Guid Id,
    string Code,
    string DisplayNameKey,
    ProjectTemplateVisibility Visibility,
    DateTime? SubmittedAtUtc,
    DateTime? PublishedAtUtc,
    string? RejectionReason,
    int DownloadCount);

public record MarketplaceReviewDto(
    Guid Id,
    Guid TemplateId,
    Guid ReviewerUserId,
    int RatingStars,
    string? CommentMd,
    DateTime ReviewedAtUtc);

public record SubmitMarketplaceDto(Guid TenantTemplateId);

public record PublishMarketplaceDto(Guid TemplateId);

public record RejectMarketplaceDto(Guid TemplateId, string Reason);

public record InstallMarketplaceDto(Guid MarketplaceTemplateId);

public record RateMarketplaceDto(Guid TemplateId, int RatingStars, string? CommentMd);

public record InstallMarketplaceResultDto(Guid InstalledTemplateId);
