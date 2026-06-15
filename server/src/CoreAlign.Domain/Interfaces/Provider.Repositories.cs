using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface ITenantProviderConfigRepository
{
    Task<TenantProviderConfig?> GetByTenantAndCategoryAsync(Guid tenantId, ProviderCategory category, string providerName, CancellationToken cancellationToken = default);
    Task<TenantProviderConfig?> GetDefaultForTenantAsync(Guid tenantId, ProviderCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantProviderConfig>> ListByTenantAsync(Guid tenantId, ProviderCategory? category, CancellationToken cancellationToken = default);
    Task AddAsync(TenantProviderConfig config, CancellationToken cancellationToken = default);
    void Update(TenantProviderConfig config);
    void Remove(TenantProviderConfig config);
}

public interface IProviderWebhookInboxRepository
{
    Task<bool> ExistsBySignatureAsync(string signatureHash, CancellationToken cancellationToken = default);
    Task AddAsync(ProviderWebhookInbox entry, CancellationToken cancellationToken = default);
    Task<ProviderWebhookInbox?> GetBySignatureAsync(string signatureHash, CancellationToken cancellationToken = default);
    Task<ProviderWebhookInbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ProviderWebhookInbox> Items, int Total)> ListAsync(
        Guid tenantId,
        string? providerName,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    void Update(ProviderWebhookInbox entry);
}
