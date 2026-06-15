using CoreAlign.Application.B2B;
using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Application.GlassEnclosure.Marketplace.Mapping;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Marketplace.Services;

public class ProjectMarketplaceService : IProjectMarketplaceService
{
    private readonly IProjectTemplateRepository _templateRepo;
    private readonly IProjectTemplateReviewRepository _reviewRepo;
    private readonly IProjectTemplateInstallRepository _installRepo;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserAccessor _currentUser;

    public ProjectMarketplaceService(
        IProjectTemplateRepository templateRepo,
        IProjectTemplateReviewRepository reviewRepo,
        IProjectTemplateInstallRepository installRepo,
        ITenantContext tenantContext,
        ICurrentUserAccessor currentUser)
    {
        _templateRepo = templateRepo;
        _reviewRepo = reviewRepo;
        _installRepo = installRepo;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<MarketplaceSubmissionDto> SubmitToMarketplaceAsync(
        Guid tenantTemplateId,
        Guid submitterUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var template = await _templateRepo.GetByIdAsync(tenantTemplateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException();

        _tenantContext.EnsureSameTenant(template.TenantId);

        if (template.IsSystemTemplate)
        {
            throw new MarketplaceCannotSubmitGlobalTemplateException();
        }

        template.SubmitToMarketplace(tenantId);
        _ = submitterUserId;
        _templateRepo.Update(template);
        return MarketplaceMappers.ToSubmission(template);
    }

    public async Task<MarketplaceSubmissionDto> PublishAsync(
        Guid templateId,
        Guid platformAdminUserId,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetByIdIgnoringTenantAsync(templateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException();

        if (template.Visibility != ProjectTemplateVisibility.MarketplaceSubmitted)
        {
            throw new MarketplaceTemplateInvalidStateException("published", template.Visibility.ToString());
        }

        template.Publish(platformAdminUserId);
        _templateRepo.Update(template);
        return MarketplaceMappers.ToSubmission(template);
    }

    public async Task<MarketplaceSubmissionDto> RejectAsync(
        Guid templateId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetByIdIgnoringTenantAsync(templateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException();

        if (template.Visibility != ProjectTemplateVisibility.MarketplaceSubmitted)
        {
            throw new MarketplaceTemplateInvalidStateException("rejected", template.Visibility.ToString());
        }

        template.Reject(reason);
        _templateRepo.Update(template);
        return MarketplaceMappers.ToSubmission(template);
    }

    public async Task<InstallMarketplaceResultDto> InstallToTenantAsync(
        Guid marketplaceTemplateId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var userId = _currentUser.UserIdOrThrow();

        var source = await _templateRepo.GetByIdWithPresetsIgnoringTenantAsync(marketplaceTemplateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException();

        if (source.Visibility != ProjectTemplateVisibility.MarketplacePublished || !source.IsActive)
        {
            throw new MarketplaceTemplateNotPublishedException();
        }

        if (source.SubmittedByTenantId == tenantId)
        {
            throw new MarketplaceCannotInstallOwnSubmissionException();
        }

        var cloneCode = $"{source.Code}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var clone = new ProjectTemplate(
            code: cloneCode,
            displayNameKey: source.DisplayNameKey,
            isSystemTemplate: false,
            category: source.Category,
            subtype: source.Subtype,
            geometryMode: source.GeometryMode,
            mountingTopology: source.MountingTopology,
            defaultConnectorKind: source.DefaultConnectorKind,
            roofPitchDeg: source.RoofPitchDeg,
            ridgeHeightMm: source.RidgeHeightMm,
            eaveHeightMm: source.EaveHeightMm,
            thumbnailUrl: source.ThumbnailUrl,
            descriptionKey: source.DescriptionKey,
            metadataJson: source.MetadataJson,
            sortOrder: source.SortOrder);

        clone.TenantId = tenantId;
        clone.MarkAsCloneOf(source.Id, tenantId);

        foreach (var preset in source.RunPresets.OrderBy(p => p.OrderIndex))
        {
            var presetClone = new ProjectTemplateRunPreset(
                templateId: clone.Id,
                orderIndex: preset.OrderIndex,
                labelKey: preset.LabelKey,
                lengthMm: preset.LengthMm,
                heightMm: preset.HeightMm,
                defaultPanelCount: preset.DefaultPanelCount,
                defaultPanelWidthMm: preset.DefaultPanelWidthMm,
                defaultOpeningType: preset.DefaultOpeningType,
                originX: preset.OriginX,
                originY: preset.OriginY,
                rotationDeg: preset.RotationDeg,
                hasTopDrip: preset.HasTopDrip,
                hasBottomThreshold: preset.HasBottomThreshold,
                connectsToPreviousAsCorner: preset.ConnectsToPreviousAsCorner,
                cornerJointAngleDeg: preset.CornerJointAngleDeg,
                cornerUsesPost: preset.CornerUsesPost)
            {
                TenantId = tenantId
            };
            clone.AddRunPreset(presetClone);
        }

        await _templateRepo.AddAsync(clone, cancellationToken);

        source.IncrementDownload();
        _templateRepo.Update(source);

        var install = new ProjectTemplateInstall(source.Id, userId, clone.Id)
        {
            TenantId = tenantId
        };
        await _installRepo.AddAsync(install, cancellationToken);

        return new InstallMarketplaceResultDto(clone.Id);
    }

    public async Task<MarketplaceReviewDto> RateAsync(
        Guid templateId,
        int ratingStars,
        string? commentMd,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var userId = _currentUser.UserIdOrThrow();

        var template = await _templateRepo.GetByIdIgnoringTenantAsync(templateId, cancellationToken)
            ?? throw new ProjectTemplateNotFoundException();

        if (template.Visibility != ProjectTemplateVisibility.MarketplacePublished || !template.IsActive)
        {
            throw new MarketplaceTemplateNotPublishedException();
        }

        var existing = await _reviewRepo.GetByTemplateAndReviewerAsync(templateId, userId, cancellationToken);
        ProjectTemplateReview review;
        if (existing is null)
        {
            review = new ProjectTemplateReview(templateId, userId, ratingStars, commentMd)
            {
                TenantId = tenantId
            };
            await _reviewRepo.AddAsync(review, cancellationToken);
        }
        else
        {
            existing.UpdateRating(ratingStars, commentMd);
            _reviewRepo.Update(existing);
            review = existing;
        }

        var (count, average) = await _reviewRepo.GetAggregateAsync(templateId, cancellationToken);
        var projectedCount = existing is null ? count + 1 : count;
        var projectedAverage = existing is null
            ? CalculateProjectedAverage(average, count, ratingStars)
            : average;

        template.RecalculateRating(projectedCount, projectedAverage);
        _templateRepo.Update(template);

        return MarketplaceMappers.ToReviewDto(review);
    }

    private static decimal? CalculateProjectedAverage(decimal? currentAverage, int currentCount, int newRating)
    {
        if (currentCount == 0 || currentAverage is null)
        {
            return newRating;
        }
        var total = (currentAverage.Value * currentCount) + newRating;
        return Math.Round(total / (currentCount + 1), 2);
    }

    public async Task<IReadOnlyList<MarketplaceTemplateSummaryDto>> ListMarketplaceAsync(
        EnclosureCategory? category,
        MarketplaceSortBy sortBy,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var safeSkip = skip < 0 ? 0 : skip;
        var safeTake = take is <= 0 or > 100 ? 20 : take;
        var templates = await _templateRepo.ListMarketplaceAsync(category, sortBy, safeSkip, safeTake, cancellationToken);
        return templates.Select(MarketplaceMappers.ToSummary).ToList();
    }

    public async Task<MarketplaceTemplateDetailDto?> GetMarketplaceTemplateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRepo.GetByIdWithPresetsIgnoringTenantAsync(id, cancellationToken);
        if (template is null || template.Visibility != ProjectTemplateVisibility.MarketplacePublished)
        {
            return null;
        }
        return MarketplaceMappers.ToDetail(template);
    }

    public async Task<IReadOnlyList<MarketplaceSubmissionDto>> ListMyTenantSubmissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var templates = await _templateRepo.ListSubmissionsByTenantAsync(tenantId, cancellationToken);
        return templates.Select(MarketplaceMappers.ToSubmission).ToList();
    }

    public async Task<IReadOnlyList<MarketplaceSubmissionDto>> ListPendingSubmissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var templates = await _templateRepo.ListPendingSubmissionsAsync(cancellationToken);
        return templates.Select(MarketplaceMappers.ToSubmission).ToList();
    }

    public async Task<IReadOnlyList<MarketplaceReviewDto>> ListReviewsAsync(
        Guid templateId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var safeSkip = skip < 0 ? 0 : skip;
        var safeTake = take is <= 0 or > 100 ? 20 : take;
        var reviews = await _reviewRepo.ListByTemplateAsync(templateId, safeSkip, safeTake, cancellationToken);
        return reviews.Select(MarketplaceMappers.ToReviewDto).ToList();
    }
}
