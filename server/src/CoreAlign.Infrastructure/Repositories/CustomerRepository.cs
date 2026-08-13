using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly CoreAlignDbContext _context;

    public CustomerRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Dictionary<Guid, Customer>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0) return new Dictionary<Guid, Customer>();
        return await _context.Customers
            .Where(c => idList.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Customer> Items, int Total)> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.Name, lower) ||
                    (c.Code != null && EF.Functions.ILike(c.Code, lower)) ||
                    (c.LegalName != null && EF.Functions.ILike(c.LegalName, lower)) ||
                    (c.Email != null && EF.Functions.ILike(c.Email, lower)) ||
                    (c.Phone != null && EF.Functions.ILike(c.Phone, lower)) ||
                    (c.TaxNumber != null && EF.Functions.ILike(c.TaxNumber, lower)));
            }
            else
            {
                query = query.Where(c =>
                    EF.Functions.Like(c.Name.ToLower(), lower) ||
                    (c.Code != null && EF.Functions.Like(c.Code.ToLower(), lower)) ||
                    (c.LegalName != null && EF.Functions.Like(c.LegalName.ToLower(), lower)) ||
                    (c.Email != null && EF.Functions.Like(c.Email.ToLower(), lower)) ||
                    (c.Phone != null && EF.Functions.Like(c.Phone.ToLower(), lower)) ||
                    (c.TaxNumber != null && EF.Functions.Like(c.TaxNumber.ToLower(), lower)));
            }
        }

        if (isActive.HasValue)
        {
            query = isActive.Value
                ? query.Where(c => c.Status == CustomerStatus.Active)
                : query.Where(c => c.Status != CustomerStatus.Active);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public void Update(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public void Remove(Customer customer)
    {
        _context.Customers.Remove(customer);
    }

    public async Task<(int OrderCount, decimal OrderTotal)> GetOrderTotalsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var stats = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => 1)
            .Select(g => new { Count = g.Count(), Total = g.Sum(o => o.Total) })
            .FirstOrDefaultAsync(cancellationToken);

        return (stats?.Count ?? 0, stats?.Total ?? 0m);
    }

    public async Task<(int InvoiceCount, decimal Invoiced, decimal Paid, decimal Outstanding, string Currency)> GetInvoiceTotalsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        // Server-side aggregate via conditional sums — avoids pulling every
        // invoice row into memory just to derive 5 scalars. Currency falls back
        // to a separate single-row query if no invoices exist yet.
        var agg = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Invoiced = g.Sum(i => i.Total),
                Paid = g.Sum(i => i.Status == InvoiceStatus.Paid ? i.Total : 0m),
                Outstanding = g.Sum(i => i.Status == InvoiceStatus.Issued ? i.Total : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (agg is null)
        {
            return (0, 0m, 0m, 0m, "USD");
        }

        var currency = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .Select(i => i.Currency)
            .FirstOrDefaultAsync(cancellationToken) ?? "USD";

        return (agg.Count, agg.Invoiced, agg.Paid, agg.Outstanding, currency);
    }

    public async Task<IReadOnlyList<DuplicateGroupRow>> FindDuplicatesAsync(
        DuplicateKeyKind key,
        CancellationToken cancellationToken = default)
    {
        var q = _context.Customers.AsNoTracking();

        if (key == DuplicateKeyKind.Email)
        {
            var groups = (await q
                .Where(c => c.Email != null && c.Email != "")
                .GroupBy(c => c.Email!.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Count))
                .ToList();
            if (groups.Count == 0) return Array.Empty<DuplicateGroupRow>();
            var keys = groups.Select(g => g.Key).ToList();
            var members = (await q
                .Where(c => c.Email != null && keys.Contains(c.Email!.ToLower()))
                .Select(c => new { Key = c.Email!.ToLower(), c.Id, c.Name })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Id, x.Name))
                .ToList();
            return DuplicateGroupAssembler.Build(groups, members);
        }

        if (key == DuplicateKeyKind.TaxNumber)
        {
            var groups = (await q
                .Where(c => c.TaxNumber != null && c.TaxNumber != "")
                .GroupBy(c => c.TaxNumber!)
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Count))
                .ToList();
            if (groups.Count == 0) return Array.Empty<DuplicateGroupRow>();
            var keys = groups.Select(g => g.Key).ToList();
            var members = (await q
                .Where(c => c.TaxNumber != null && keys.Contains(c.TaxNumber!))
                .Select(c => new { Key = c.TaxNumber!, c.Id, c.Name })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Id, x.Name))
                .ToList();
            return DuplicateGroupAssembler.Build(groups, members);
        }

        var nidGroups = (await q
            .Where(c => c.NationalId != null && c.NationalId != "")
            .GroupBy(c => c.NationalId!)
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => (x.Key, x.Count))
            .ToList();
        if (nidGroups.Count == 0) return Array.Empty<DuplicateGroupRow>();
        var nidKeys = nidGroups.Select(g => g.Key).ToList();
        var nidMembers = (await q
            .Where(c => c.NationalId != null && nidKeys.Contains(c.NationalId!))
            .Select(c => new { Key = c.NationalId!, c.Id, c.Name })
            .ToListAsync(cancellationToken))
            .Select(x => (x.Key, x.Id, x.Name))
            .ToList();
        return DuplicateGroupAssembler.Build(nidGroups, nidMembers);
    }

    // Advisory point lookup for the entry form: it warns, it never blocks, so a legitimate second
    // record under the same identity is still possible — the operator decides.
    public async Task<IReadOnlyList<DuplicateMemberRow>> FindByIdentityAsync(
        string? taxNumber,
        string? nationalId,
        string? email,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        var tax = Blank(taxNumber);
        var national = Blank(nationalId);
        var mail = Blank(email)?.ToLowerInvariant();
        if (tax is null && national is null && mail is null) return Array.Empty<DuplicateMemberRow>();

        var rows = await _context.Customers
            .AsNoTracking()
            .Where(c => (tax != null && c.TaxNumber == tax)
                || (national != null && c.NationalId == national)
                || (mail != null && c.Email != null && EF.Functions.ILike(c.Email, mail)))
            .Where(c => excludeId == null || c.Id != excludeId)
            .OrderBy(c => c.Name)
            .Take(10)
            .Select(c => new DuplicateMemberRow(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
