using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetByExternalTransactionIdAsync(string providerName, string externalTransactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-tenant lookup used by 3DS bank callbacks and webhook entry points that
    /// arrive without an authenticated request principal. Bypasses the tenant query
    /// filter so the caller can establish a tenant scope from the returned row.
    /// MUST only be used at trust boundaries that subsequently push a tenant scope
    /// matching <see cref="PaymentTransaction.TenantId"/>.
    /// </summary>
    Task<PaymentTransaction?> GetByExternalIdGlobalAsync(string providerName, string externalTransactionId, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> ListPendingForTenantAsync(Guid tenantId, int max, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> ListByStatusAsync(Guid tenantId, PaymentTransactionStatus status, int max, CancellationToken cancellationToken = default);

    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);

    void Update(PaymentTransaction transaction);
}
