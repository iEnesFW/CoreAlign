using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>
/// Thread-safe in-memory <see cref="IPaymentTransactionRepository"/>. Mirrors EF Core
/// semantics closely enough for dispatcher integration tests: AddAsync stores, Update is a no-op
/// because the entity reference is already tracked, and a duplicate add is rejected
/// (mirrors the ux_payment_transactions_tenant_idempotency_key unique index on
/// TenantId+IdempotencyKey, filtered to non-null keys, that the production DbContext enforces).
/// </summary>
public sealed class InMemoryPaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly List<PaymentTransaction> _store = new();
    private readonly object _lock = new();
    private readonly HashSet<string> _idempotencyKeys = new(StringComparer.Ordinal);

    public IReadOnlyList<PaymentTransaction> Snapshot
    {
        get
        {
            lock (_lock) { return _store.ToList(); }
        }
    }

    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!string.IsNullOrWhiteSpace(transaction.IdempotencyKey)
                && !_idempotencyKeys.Add($"{transaction.TenantId:N}|{transaction.IdempotencyKey}"))
            {
                throw new InvalidOperationException(
                    $"Duplicate PaymentTransaction insert rejected (tenant {transaction.TenantId:N}, idempotency key {transaction.IdempotencyKey}).");
            }
            _store.Add(transaction);
            AddCount++;
        }
        return Task.CompletedTask;
    }

    public void Update(PaymentTransaction transaction)
    {
        UpdateCount++;
    }

    public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.FirstOrDefault(t => t.Id == id));
        }
    }

    public Task<PaymentTransaction?> GetByExternalTransactionIdAsync(string providerName, string externalTransactionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var hit = _store.FirstOrDefault(t =>
                string.Equals(t.ProviderName, providerName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(t.ExternalTransactionId, externalTransactionId, StringComparison.Ordinal));
            return Task.FromResult(hit);
        }
    }

    public Task<PaymentTransaction?> GetByExternalIdGlobalAsync(string providerName, string externalTransactionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var match = _store.FirstOrDefault(t =>
                t.ProviderName == providerName &&
                t.ExternalTransactionId == externalTransactionId);
            return Task.FromResult<PaymentTransaction?>(match);
        }
    }

    public Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Task.FromResult<PaymentTransaction?>(null);
        }
        lock (_lock)
        {
            var hit = _store.FirstOrDefault(t =>
                t.TenantId == tenantId
                && string.Equals(t.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
            return Task.FromResult(hit);
        }
    }

    public Task<IReadOnlyList<PaymentTransaction>> ListPendingForTenantAsync(Guid tenantId, int max, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<PaymentTransaction> result = _store
                .Where(t => t.TenantId == tenantId && t.Status == PaymentTransactionStatus.Pending)
                .Take(max)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<PaymentTransaction>> ListByStatusAsync(Guid tenantId, PaymentTransactionStatus status, int max, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<PaymentTransaction> result = _store
                .Where(t => t.TenantId == tenantId && t.Status == status)
                .Take(max)
                .ToList();
            return Task.FromResult(result);
        }
    }
}
