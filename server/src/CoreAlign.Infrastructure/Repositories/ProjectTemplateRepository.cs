using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ProjectTemplateRepository : IProjectTemplateRepository
{
    private readonly CoreAlignDbContext _context;

    public ProjectTemplateRepository(CoreAlignDbContext context) => _context = context;

    public Task<ProjectTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ProjectTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<ProjectTemplate?> GetByIdWithPresetsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ProjectTemplates
            .Include(t => t.RunPresets)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<ProjectTemplate?> GetByIdIgnoringTenantAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ProjectTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<ProjectTemplate?> GetByIdWithPresetsIgnoringTenantAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ProjectTemplates
            .IgnoreQueryFilters()
            .Include(t => t.RunPresets)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<ProjectTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.ProjectTemplates.FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ProjectTemplate>> ListAsync(
        EnclosureCategory? category,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectTemplates.AsQueryable();
        if (category.HasValue) query = query.Where(t => t.Category == category.Value);
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        return await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectTemplate>> ListMarketplaceAsync(
        EnclosureCategory? category,
        MarketplaceSortBy sortBy,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Visibility == ProjectTemplateVisibility.MarketplacePublished && t.IsActive);
        if (category.HasValue) query = query.Where(t => t.Category == category.Value);

        query = sortBy switch
        {
            MarketplaceSortBy.Recent => query.OrderByDescending(t => t.PublishedAtUtc),
            MarketplaceSortBy.Rating => query.OrderByDescending(t => t.AverageRating ?? 0m).ThenByDescending(t => t.ReviewCount),
            _ => query.OrderByDescending(t => t.DownloadCount).ThenByDescending(t => t.AverageRating ?? 0m),
        };

        return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectTemplate>> ListSubmissionsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        await _context.ProjectTemplates
            .IgnoreQueryFilters()
            .Where(t => t.SubmittedByTenantId == tenantId
                && (t.Visibility == ProjectTemplateVisibility.MarketplaceSubmitted
                    || t.Visibility == ProjectTemplateVisibility.MarketplacePublished
                    || t.Visibility == ProjectTemplateVisibility.MarketplaceRejected))
            .OrderByDescending(t => t.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectTemplate>> ListPendingSubmissionsAsync(CancellationToken cancellationToken = default) =>
        await _context.ProjectTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Visibility == ProjectTemplateVisibility.MarketplaceSubmitted)
            .OrderBy(t => t.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProjectTemplate template, CancellationToken cancellationToken = default) =>
        await _context.ProjectTemplates.AddAsync(template, cancellationToken);

    public void Update(ProjectTemplate template) => _context.ProjectTemplates.Update(template);

    public void Remove(ProjectTemplate template) => _context.ProjectTemplates.Remove(template);
}

public class ProjectTemplateReviewRepository : IProjectTemplateReviewRepository
{
    private readonly CoreAlignDbContext _context;

    public ProjectTemplateReviewRepository(CoreAlignDbContext context) => _context = context;

    public Task<ProjectTemplateReview?> GetByTemplateAndReviewerAsync(Guid templateId, Guid reviewerUserId, CancellationToken cancellationToken = default) =>
        _context.ProjectTemplateReviews.FirstOrDefaultAsync(
            r => r.TemplateId == templateId && r.ReviewerUserId == reviewerUserId,
            cancellationToken);

    public async Task<IReadOnlyList<ProjectTemplateReview>> ListByTemplateAsync(Guid templateId, int skip, int take, CancellationToken cancellationToken = default) =>
        await _context.ProjectTemplateReviews
            .IgnoreQueryFilters()
            .Where(r => r.TemplateId == templateId)
            .OrderByDescending(r => r.ReviewedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<(int Count, decimal? Average)> GetAggregateAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectTemplateReviews.IgnoreQueryFilters().Where(r => r.TemplateId == templateId);
        var count = await query.CountAsync(cancellationToken);
        if (count == 0)
        {
            return (0, null);
        }
        var avg = await query.AverageAsync(r => (decimal)r.RatingStars, cancellationToken);
        return (count, Math.Round(avg, 2));
    }

    public async Task AddAsync(ProjectTemplateReview review, CancellationToken cancellationToken = default) =>
        await _context.ProjectTemplateReviews.AddAsync(review, cancellationToken);

    public void Update(ProjectTemplateReview review) => _context.ProjectTemplateReviews.Update(review);
}

public class ProjectTemplateInstallRepository : IProjectTemplateInstallRepository
{
    private readonly CoreAlignDbContext _context;

    public ProjectTemplateInstallRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(ProjectTemplateInstall install, CancellationToken cancellationToken = default) =>
        await _context.ProjectTemplateInstalls.AddAsync(install, cancellationToken);

    public Task<int> CountByTemplateAsync(Guid marketplaceTemplateId, CancellationToken cancellationToken = default) =>
        _context.ProjectTemplateInstalls
            .IgnoreQueryFilters()
            .CountAsync(i => i.MarketplaceTemplateId == marketplaceTemplateId, cancellationToken);

    public async Task<IReadOnlyList<ProjectTemplateInstall>> ListByTenantAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default) =>
        await _context.ProjectTemplateInstalls
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.InstalledAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
}
