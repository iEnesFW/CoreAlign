using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly CoreAlignDbContext _context;

    public QuoteRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Quote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Lines)
            .AsSplitQuery()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public Task<Quote?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Lines)
            .ThenInclude(l => l.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public Task<bool> QuoteNumberExistsAsync(string quoteNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Quotes.Where(q => q.QuoteNumber == quoteNumber);
        if (excludeId.HasValue)
        {
            query = query.Where(q => q.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task AcquireConversionLockAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql())
        {
            return;
        }
        var key = quoteId.ToString();
        await _context.Database
            .ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
                cancellationToken);
    }

    public async Task<(IReadOnlyList<QuoteSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        QuoteStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Quotes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(q =>
                    EF.Functions.ILike(q.QuoteNumber, lower) ||
                    EF.Functions.ILike(q.Customer.Name, lower));
            }
            else
            {
                query = query.Where(q =>
                    EF.Functions.Like(q.QuoteNumber.ToLower(), lower) ||
                    EF.Functions.Like(q.Customer.Name.ToLower(), lower));
            }
        }

        if (customerId.HasValue)
        {
            query = query.Where(q => q.CustomerId == customerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.QuoteDate)
            .ThenBy(q => q.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuoteSearchRow(
                q.Id,
                q.QuoteNumber,
                q.CustomerId,
                q.Customer != null ? q.Customer.Name : q.CustomerSnapshot != null ? q.CustomerSnapshot.LegalName : string.Empty,
                q.QuoteDate,
                q.ValidUntilUtc,
                q.Status,
                q.Currency,
                q.Total,
                q.ConvertedOrderId))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Quote>> GetExpirableSentQuotesAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.Quotes
            .IgnoreQueryFilters()
            .Where(q => q.Status == QuoteStatus.Sent && q.ValidUntilUtc < nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Quote quote, CancellationToken cancellationToken = default)
    {
        await _context.Quotes.AddAsync(quote, cancellationToken);
    }

    public void Update(Quote quote) => _context.Quotes.Update(quote);

    public void Remove(Quote quote) => _context.Quotes.Remove(quote);
}
