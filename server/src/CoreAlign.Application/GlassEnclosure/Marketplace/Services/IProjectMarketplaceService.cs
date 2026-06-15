using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.Services;

public interface IProjectMarketplaceService
{
    Task<MarketplaceSubmissionDto> SubmitToMarketplaceAsync(
        Guid tenantTemplateId,
        Guid submitterUserId,
        CancellationToken cancellationToken = default);

    Task<MarketplaceSubmissionDto> PublishAsync(
        Guid templateId,
        Guid platformAdminUserId,
        CancellationToken cancellationToken = default);

    Task<MarketplaceSubmissionDto> RejectAsync(
        Guid templateId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<InstallMarketplaceResultDto> InstallToTenantAsync(
        Guid marketplaceTemplateId,
        CancellationToken cancellationToken = default);

    Task<MarketplaceReviewDto> RateAsync(
        Guid templateId,
        int ratingStars,
        string? commentMd,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceTemplateSummaryDto>> ListMarketplaceAsync(
        EnclosureCategory? category,
        MarketplaceSortBy sortBy,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<MarketplaceTemplateDetailDto?> GetMarketplaceTemplateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceSubmissionDto>> ListMyTenantSubmissionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceSubmissionDto>> ListPendingSubmissionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceReviewDto>> ListReviewsAsync(
        Guid templateId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
