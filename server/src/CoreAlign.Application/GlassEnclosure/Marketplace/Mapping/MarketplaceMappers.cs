using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.Mapping;

public static class MarketplaceMappers
{
    public static MarketplaceTemplateSummaryDto ToSummary(ProjectTemplate template) => new(
        template.Id,
        template.Code,
        template.DisplayNameKey,
        template.Category,
        template.Subtype,
        template.ThumbnailUrl,
        template.DescriptionKey,
        template.SubmittedByTenantId,
        template.PublishedAtUtc,
        template.DownloadCount,
        template.AverageRating,
        template.ReviewCount);

    public static MarketplaceTemplateDetailDto ToDetail(ProjectTemplate template) => new(
        template.Id,
        template.Code,
        template.DisplayNameKey,
        template.Category,
        template.Subtype,
        template.GeometryMode,
        template.MountingTopology,
        template.DefaultConnectorKind,
        template.RoofPitchDeg,
        template.RidgeHeightMm,
        template.EaveHeightMm,
        template.ThumbnailUrl,
        template.DescriptionKey,
        template.MetadataJson,
        template.Visibility,
        template.SubmittedByTenantId,
        template.SubmittedAtUtc,
        template.PublishedAtUtc,
        template.DownloadCount,
        template.AverageRating,
        template.ReviewCount,
        template.RejectionReason,
        template.RunPresets.Count);

    public static MarketplaceSubmissionDto ToSubmission(ProjectTemplate template) => new(
        template.Id,
        template.Code,
        template.DisplayNameKey,
        template.Visibility,
        template.SubmittedAtUtc,
        template.PublishedAtUtc,
        template.RejectionReason,
        template.DownloadCount);

    public static MarketplaceReviewDto ToReviewDto(ProjectTemplateReview review) => new(
        review.Id,
        review.TemplateId,
        review.ReviewerUserId,
        review.RatingStars,
        review.CommentMd,
        review.ReviewedAtUtc);
}
