using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class IncomingInvoiceRepository : IIncomingInvoiceRepository
{
    private readonly CoreAlignDbContext _context;

    public IncomingInvoiceRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(IncomingInvoice invoice, CancellationToken cancellationToken = default)
        => await _context.Set<IncomingInvoice>().AddAsync(invoice, cancellationToken);

    public Task<IncomingInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Set<IncomingInvoice>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByEttnAsync(string ettn, CancellationToken cancellationToken = default)
        => _context.Set<IncomingInvoice>().AnyAsync(x => x.Ettn == ettn, cancellationToken);

    public async Task<IReadOnlyList<IncomingInvoice>> ExistingEttnsAsync(IEnumerable<string> ettns, CancellationToken cancellationToken = default)
    {
        var list = ettns?.Distinct().ToArray() ?? [];
        if (list.Length == 0) return [];
        return await _context.Set<IncomingInvoice>()
            .AsNoTracking()
            .Where(x => list.Contains(x.Ettn))
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<IncomingInvoice> Items, int Total)> SearchAsync(
        IncomingInvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<IncomingInvoice>().AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
