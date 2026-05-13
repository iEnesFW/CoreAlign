using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class AccountingPeriodRepository : IAccountingPeriodRepository
{
    private readonly CoreAlignDbContext _context;
    public AccountingPeriodRepository(CoreAlignDbContext context) => _context = context;

    public Task<AccountingPeriod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.AccountingPeriods.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<AccountingPeriod?> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default) =>
        _context.AccountingPeriods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, cancellationToken);

    public Task<AccountingPeriod?> GetByDateAsync(DateTime postingDate, CancellationToken cancellationToken = default) =>
        _context.AccountingPeriods.FirstOrDefaultAsync(
            p => p.Year == postingDate.Year && p.Month == postingDate.Month, cancellationToken);

    public async Task<IReadOnlyList<AccountingPeriod>> ListAsync(int? year = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AccountingPeriods.AsNoTracking().AsQueryable();
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);
        return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync(cancellationToken);
    }

    public async Task<AccountingPeriod> GetOrCreateForDateAsync(DateTime postingDate, CancellationToken cancellationToken = default)
    {
        var existing = await GetByDateAsync(postingDate, cancellationToken);
        if (existing is not null) return existing;
        var period = new AccountingPeriod(postingDate.Year, postingDate.Month);
        await _context.AccountingPeriods.AddAsync(period, cancellationToken);
        return period;
    }

    public async Task AddAsync(AccountingPeriod period, CancellationToken cancellationToken = default) =>
        await _context.AccountingPeriods.AddAsync(period, cancellationToken);

    public void Update(AccountingPeriod period) => _context.AccountingPeriods.Update(period);
}

public class CustomerProductPriceRepository : ICustomerProductPriceRepository
{
    private readonly CoreAlignDbContext _context;
    public CustomerProductPriceRepository(CoreAlignDbContext context) => _context = context;

    public Task<CustomerProductPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.CustomerProductPrices.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerProductPrice>> GetForCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default) =>
        await _context.CustomerProductPrices
            .Where(p => p.CustomerId == customerId && p.ProductId == productId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerProductPrice>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.CustomerProductPrices
            .Include(p => p.Product)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerProductPrice>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        await _context.CustomerProductPrices
            .Include(p => p.Customer)
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CustomerProductPrice price, CancellationToken cancellationToken = default) =>
        await _context.CustomerProductPrices.AddAsync(price, cancellationToken);

    public void Update(CustomerProductPrice price) => _context.CustomerProductPrices.Update(price);
    public void Remove(CustomerProductPrice price) => _context.CustomerProductPrices.Remove(price);
}
