using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class CustomerTransactionRepository : ICustomerTransactionRepository
{
    private readonly CoreAlignDbContext _context;

    public CustomerTransactionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CustomerTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.CustomerTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<(IReadOnlyList<CustomerTransaction> Items, int Total)> GetByCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerTransactions.AsNoTracking().Where(t => t.CustomerId == customerId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}

public class StockTransactionRepository : IStockTransactionRepository
{
    private readonly CoreAlignDbContext _context;

    public StockTransactionRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(StockTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.StockTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<(IReadOnlyList<StockTransaction> Items, int Total)> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.StockTransactions.AsNoTracking().Where(t => t.ProductId == productId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
