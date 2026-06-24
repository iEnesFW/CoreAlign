using CoreAlign.Application.Compliance.Audit;
using CoreAlign.Domain.Entities.Compliance;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Compliance;

public sealed class AuditChainVerifier : IAuditChainVerifier
{
    private readonly CoreAlignDbContext _context;

    public AuditChainVerifier(CoreAlignDbContext context) => _context = context;

    public async Task<AuditChainVerificationResult> VerifyTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        string? previousHash = null;
        long count = 0;

        var query = _context.EntityAuditLogs
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(l => l.TenantId == tenantId)
            .OrderBy(l => l.Sequence)
            .AsAsyncEnumerable();

        await foreach (var row in query.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var expected = EntityAuditLog.ComputeRollingHash(
                previousHash,
                row.TenantId,
                row.EntityType,
                row.EntityId,
                row.Action,
                row.BeforeJson,
                row.AfterJson,
                row.UserId,
                row.ChangedAtUtc,
                row.Sequence);

            if (!string.Equals(expected, row.RollingHash, StringComparison.Ordinal))
            {
                return new AuditChainVerificationResult(
                    tenantId,
                    count,
                    IsValid: false,
                    FirstBrokenSequence: row.Sequence,
                    Detail: $"Rolling-hash mismatch at sequence {row.Sequence} (entity {row.EntityType}:{row.EntityId}).");
            }

            previousHash = row.RollingHash;
            count++;
        }

        return new AuditChainVerificationResult(tenantId, count, IsValid: true, FirstBrokenSequence: null, Detail: null);
    }
}
