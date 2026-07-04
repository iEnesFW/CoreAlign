using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class GibCodeRepository : IGibCodeRepository
{
    private readonly CoreAlignDbContext _context;

    public GibCodeRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<WithholdingTaxCode>> ListWithholdingCodesAsync(bool? isActive, CancellationToken ct = default)
    {
        var query = _context.WithholdingTaxCodes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == Guid.Empty);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        return await query.OrderBy(x => x.Code).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<VatExemptionCode>> ListExemptionCodesAsync(bool? isActive, CancellationToken ct = default)
    {
        var query = _context.VatExemptionCodes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == Guid.Empty);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        return await query.OrderBy(x => x.Code).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, WithholdingTaxCode>> GetWithholdingByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids?.Distinct().ToArray() ?? [];
        if (idList.Length == 0) return new Dictionary<Guid, WithholdingTaxCode>();
        return await _context.WithholdingTaxCodes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == Guid.Empty && idList.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
    }

    public Task<VatExemptionCode?> GetExemptionByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.VatExemptionCodes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == Guid.Empty && x.Id == id, ct);
}
