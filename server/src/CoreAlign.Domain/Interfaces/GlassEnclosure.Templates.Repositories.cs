using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public enum MarketplaceSortBy
{
    Popularity = 0,
    Recent = 1,
    Rating = 2
}

public interface IProjectTemplateRepository
{
    Task<ProjectTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectTemplate?> GetByIdWithPresetsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectTemplate?> GetByIdIgnoringTenantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectTemplate?> GetByIdWithPresetsIgnoringTenantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTemplate>> ListAsync(
        EnclosureCategory? category,
        bool? isActive,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTemplate>> ListMarketplaceAsync(
        EnclosureCategory? category,
        MarketplaceSortBy sortBy,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTemplate>> ListSubmissionsByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTemplate>> ListPendingSubmissionsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ProjectTemplate template, CancellationToken cancellationToken = default);
    void Update(ProjectTemplate template);
    void Remove(ProjectTemplate template);
}

public interface IProjectTemplateReviewRepository
{
    Task<ProjectTemplateReview?> GetByTemplateAndReviewerAsync(Guid templateId, Guid reviewerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTemplateReview>> ListByTemplateAsync(Guid templateId, int skip, int take, CancellationToken cancellationToken = default);
    Task<(int Count, decimal? Average)> GetAggregateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectTemplateReview review, CancellationToken cancellationToken = default);
    void Update(ProjectTemplateReview review);
}

public interface IProjectTemplateInstallRepository
{
    Task AddAsync(ProjectTemplateInstall install, CancellationToken cancellationToken = default);
    Task<int> CountByTemplateAsync(Guid marketplaceTemplateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTemplateInstall>> ListByTenantAsync(Guid tenantId, int skip, int take, CancellationToken cancellationToken = default);
}
