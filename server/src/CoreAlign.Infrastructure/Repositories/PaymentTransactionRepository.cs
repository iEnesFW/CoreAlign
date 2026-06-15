using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly CoreAlignDbContext _context;

    public PaymentTransactionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<PaymentTransaction?> GetByExternalTransactionIdAsync(string providerName, string externalTransactionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(externalTransactionId))
        {
            return Task.FromResult<PaymentTransaction?>(null);
        }
        return _context.PaymentTransactions
            .FirstOrDefaultAsync(
                t => t.ProviderName == providerName && t.ExternalTransactionId == externalTransactionId,
                cancellationToken);
    }

    public Task<PaymentTransaction?> GetByExternalIdGlobalAsync(string providerName, string externalTransactionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(externalTransactionId))
        {
            return Task.FromResult<PaymentTransaction?>(null);
        }
        return _context.PaymentTransactions
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted
                && t.ProviderName == providerName
                && t.ExternalTransactionId == externalTransactionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Task.FromResult<PaymentTransaction?>(null);
        }
        var key = idempotencyKey.Trim();
        return _context.PaymentTransactions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId && t.IdempotencyKey == key && !t.IsDeleted,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> ListPendingForTenantAsync(Guid tenantId, int max, CancellationToken cancellationToken = default) =>
        await _context.PaymentTransactions
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId
                && !t.IsDeleted
                && (t.Status == PaymentTransactionStatus.Pending || t.Status == PaymentTransactionStatus.Authorized))
            .OrderBy(t => t.AttemptedAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentTransaction>> ListByStatusAsync(Guid tenantId, PaymentTransactionStatus status, int max, CancellationToken cancellationToken = default) =>
        await _context.PaymentTransactions
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.Status == status)
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Take(max)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default) =>
        await _context.PaymentTransactions.AddAsync(transaction, cancellationToken);

    public void Update(PaymentTransaction transaction) =>
        _context.PaymentTransactions.Update(transaction);
}
