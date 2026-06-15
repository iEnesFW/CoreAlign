using CoreAlign.Domain.Entities.Pricing;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories.Pricing;

public class PricingDiscountRuleRepository : IPricingDiscountRuleRepository
{
    private readonly CoreAlignDbContext _context;
    public PricingDiscountRuleRepository(CoreAlignDbContext context) => _context = context;

    public Task<DiscountRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PricingDiscountRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<DiscountRule?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.PricingDiscountRules.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<DiscountRule>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PricingDiscountRules.AsNoTracking();
        if (isActive.HasValue) query = query.Where(r => r.IsActive == isActive.Value);
        return await query
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiscountRule>> ListActiveAtAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        return await _context.PricingDiscountRules
            .AsNoTracking()
            .Where(r => r.IsActive
                && (r.ValidFromUtc == null || r.ValidFromUtc <= asOfUtc)
                && (r.ValidUntilUtc == null || r.ValidUntilUtc >= asOfUtc))
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DiscountRule rule, CancellationToken cancellationToken = default) =>
        await _context.PricingDiscountRules.AddAsync(rule, cancellationToken);

    public void Update(DiscountRule rule) => _context.PricingDiscountRules.Update(rule);
    public void Remove(DiscountRule rule) => _context.PricingDiscountRules.Remove(rule);
}

public class TaxRuleRepository : ITaxRuleRepository
{
    private readonly CoreAlignDbContext _context;
    public TaxRuleRepository(CoreAlignDbContext context) => _context = context;

    public Task<TaxRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PricingTaxRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<TaxRule?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.PricingTaxRules.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<TaxRule>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PricingTaxRules.AsNoTracking();
        if (isActive.HasValue) query = query.Where(r => r.IsActive == isActive.Value);
        return await query
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxRule>> ListActiveAtAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        return await _context.PricingTaxRules
            .AsNoTracking()
            .Where(r => r.IsActive
                && (r.ValidFromUtc == null || r.ValidFromUtc <= asOfUtc)
                && (r.ValidUntilUtc == null || r.ValidUntilUtc >= asOfUtc))
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaxRule rule, CancellationToken cancellationToken = default) =>
        await _context.PricingTaxRules.AddAsync(rule, cancellationToken);

    public void Update(TaxRule rule) => _context.PricingTaxRules.Update(rule);
    public void Remove(TaxRule rule) => _context.PricingTaxRules.Remove(rule);
}
