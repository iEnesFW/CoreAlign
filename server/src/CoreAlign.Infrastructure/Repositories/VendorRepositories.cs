using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorRepository(CoreAlignDbContext context) => _context = context;

    public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Vendors.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vendor?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.Vendors.FirstOrDefaultAsync(v => v.Code == code, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Vendors.AsNoTracking().Where(v => v.Code == code);
        if (excludeId.HasValue) query = query.Where(v => v.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> TaxNumberExistsAsync(string taxNumber, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Vendors.AsNoTracking().Where(v => v.TaxNumber == taxNumber);
        if (excludeId.HasValue) query = query.Where(v => v.Id != excludeId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public Task<Vendor?> GetByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default)
        => _context.Vendors.FirstOrDefaultAsync(v => v.TaxNumber == taxNumber, cancellationToken);

    public async Task<(IReadOnlyList<VendorSearchRow> Items, int Total)> SearchAsync(
        string? search,
        VendorStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Vendors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(v =>
                    EF.Functions.ILike(v.Name, lower) ||
                    (v.Code != null && EF.Functions.ILike(v.Code, lower)) ||
                    (v.LegalName != null && EF.Functions.ILike(v.LegalName, lower)) ||
                    (v.TaxNumber != null && EF.Functions.ILike(v.TaxNumber, lower)) ||
                    (v.Email != null && EF.Functions.ILike(v.Email, lower)) ||
                    (v.Phone != null && EF.Functions.ILike(v.Phone, lower)));
            }
            else
            {
                query = query.Where(v =>
                    EF.Functions.Like(v.Name.ToLower(), lower) ||
                    (v.Code != null && EF.Functions.Like(v.Code.ToLower(), lower)) ||
                    (v.LegalName != null && EF.Functions.Like(v.LegalName.ToLower(), lower)) ||
                    (v.TaxNumber != null && EF.Functions.Like(v.TaxNumber.ToLower(), lower)) ||
                    (v.Email != null && EF.Functions.Like(v.Email.ToLower(), lower)) ||
                    (v.Phone != null && EF.Functions.Like(v.Phone.ToLower(), lower)));
            }
        }
        if (status.HasValue) query = query.Where(v => v.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.CreatedAtUtc)
            .ThenBy(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendorSearchRow(
                v.Id,
                v.Code,
                v.Name,
                v.LegalName,
                v.TaxNumber,
                v.Email,
                v.Phone,
                v.Type,
                v.Status,
                v.DefaultCurrency,
                v.CurrentBalance,
                v.OverdueAmount))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Vendor vendor, CancellationToken cancellationToken = default) =>
        await _context.Vendors.AddAsync(vendor, cancellationToken);

    public void Update(Vendor vendor) => _context.Vendors.Update(vendor);
    public void Remove(Vendor vendor) => _context.Vendors.Remove(vendor);

    public async Task<IReadOnlyList<DuplicateGroupRow>> FindDuplicatesAsync(
        DuplicateKeyKind key,
        CancellationToken cancellationToken = default)
    {
        var q = _context.Vendors.AsNoTracking();

        if (key == DuplicateKeyKind.Email)
        {
            var groups = (await q
                .Where(v => v.Email != null && v.Email != "")
                .GroupBy(v => v.Email!.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Count))
                .ToList();
            if (groups.Count == 0) return Array.Empty<DuplicateGroupRow>();
            var keys = groups.Select(g => g.Key).ToList();
            var members = (await q
                .Where(v => v.Email != null && keys.Contains(v.Email!.ToLower()))
                .Select(v => new { Key = v.Email!.ToLower(), v.Id, v.Name })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Id, x.Name))
                .ToList();
            return DuplicateGroupAssembler.Build(groups, members);
        }

        if (key == DuplicateKeyKind.TaxNumber)
        {
            var groups = (await q
                .Where(v => v.TaxNumber != null && v.TaxNumber != "")
                .GroupBy(v => v.TaxNumber!)
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Count))
                .ToList();
            if (groups.Count == 0) return Array.Empty<DuplicateGroupRow>();
            var keys = groups.Select(g => g.Key).ToList();
            var members = (await q
                .Where(v => v.TaxNumber != null && keys.Contains(v.TaxNumber!))
                .Select(v => new { Key = v.TaxNumber!, v.Id, v.Name })
                .ToListAsync(cancellationToken))
                .Select(x => (x.Key, x.Id, x.Name))
                .ToList();
            return DuplicateGroupAssembler.Build(groups, members);
        }

        var nidGroups = (await q
            .Where(v => v.NationalId != null && v.NationalId != "")
            .GroupBy(v => v.NationalId!)
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .Select(x => (x.Key, x.Count))
            .ToList();
        if (nidGroups.Count == 0) return Array.Empty<DuplicateGroupRow>();
        var nidKeys = nidGroups.Select(g => g.Key).ToList();
        var nidMembers = (await q
            .Where(v => v.NationalId != null && nidKeys.Contains(v.NationalId!))
            .Select(v => new { Key = v.NationalId!, v.Id, v.Name })
            .ToListAsync(cancellationToken))
            .Select(x => (x.Key, x.Id, x.Name))
            .ToList();
        return DuplicateGroupAssembler.Build(nidGroups, nidMembers);
    }
}

public class VendorAddressRepository : IVendorAddressRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorAddressRepository(CoreAlignDbContext context) => _context = context;

    public Task<VendorAddress?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.VendorAddresses.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<VendorAddress>> GetByVendorAsync(Guid vendorId, CancellationToken ct = default) =>
        await _context.VendorAddresses
            .AsNoTracking()
            .Where(a => a.VendorId == vendorId)
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.Label)
            .ToListAsync(ct);

    public async Task AddAsync(VendorAddress address, CancellationToken ct = default) =>
        await _context.VendorAddresses.AddAsync(address, ct);

    public void Update(VendorAddress address) => _context.VendorAddresses.Update(address);
    public void Remove(VendorAddress address) => _context.VendorAddresses.Remove(address);
}

public class VendorContactRepository : IVendorContactRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorContactRepository(CoreAlignDbContext context) => _context = context;

    public Task<VendorContact?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.VendorContacts.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<VendorContact>> GetByVendorAsync(Guid vendorId, CancellationToken ct = default) =>
        await _context.VendorContacts
            .AsNoTracking()
            .Where(c => c.VendorId == vendorId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task AddAsync(VendorContact contact, CancellationToken ct = default) =>
        await _context.VendorContacts.AddAsync(contact, ct);

    public void Update(VendorContact contact) => _context.VendorContacts.Update(contact);
    public void Remove(VendorContact contact) => _context.VendorContacts.Remove(contact);
}

public class VendorBankAccountRepository : IVendorBankAccountRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorBankAccountRepository(CoreAlignDbContext context) => _context = context;

    public Task<VendorBankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.VendorBankAccounts.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<VendorBankAccount>> GetByVendorAsync(Guid vendorId, CancellationToken ct = default) =>
        await _context.VendorBankAccounts
            .AsNoTracking()
            .Where(b => b.VendorId == vendorId)
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.BankName)
            .ToListAsync(ct);

    public async Task AddAsync(VendorBankAccount account, CancellationToken ct = default) =>
        await _context.VendorBankAccounts.AddAsync(account, ct);

    public void Update(VendorBankAccount account) => _context.VendorBankAccounts.Update(account);
    public void Remove(VendorBankAccount account) => _context.VendorBankAccounts.Remove(account);
}

public class VendorLedgerRepository : IVendorLedgerRepository
{
    private readonly CoreAlignDbContext _context;
    public VendorLedgerRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(VendorLedgerEntry entry, CancellationToken ct = default) =>
        await _context.VendorLedgerEntries.AddAsync(entry, ct);

    public async Task AcquireAppendLockAsync(Guid vendorId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql()) return;
        var key = $"ledger:vendor:{vendorId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
    }

    public async Task<(IReadOnlyList<VendorLedgerEntry> Items, int Total)> SearchByVendorAsync(
        Guid vendorId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.VendorLedgerEntries.AsNoTracking().Where(e => e.VendorId == vendorId);
        if (fromUtc.HasValue) query = query.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(e => e.OccurredAtUtc <= toUtc.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.OccurredAtUtc)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<decimal> GetCurrentBalanceAsync(Guid vendorId, CancellationToken ct = default)
    {
        // Conventions: vendor balance is "amount we owe vendor" = sum(credit) - sum(debit).
        var row = await _context.VendorLedgerEntries
            .AsNoTracking()
            .Where(e => e.VendorId == vendorId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Debit = g.Sum(e => e.EntryType == LedgerEntryType.Debit ? e.Amount : 0m),
                Credit = g.Sum(e => e.EntryType == LedgerEntryType.Credit ? e.Amount : 0m),
            })
            .FirstOrDefaultAsync(ct);
        if (row is null) return 0m;
        return Math.Round(row.Credit - row.Debit, 4);
    }

    public async Task<decimal> GetLastRunningBalanceAsync(Guid vendorId, CancellationToken ct = default)
    {
        var last = await _context.VendorLedgerEntries
            .AsNoTracking()
            .Where(e => e.VendorId == vendorId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .FirstOrDefaultAsync(ct);
        return last?.RunningBalanceAfter ?? 0m;
    }

    public async Task<decimal> GetTotalBalanceAsOfAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        // Aggregate over ALL vendors (no per-vendor Where). Filters on PostingDate
        // to align with the GL's PostingDate cutoff for a true as-of reconciliation.
        // Vendor convention: balance "we owe" = Σ credit − Σ debit.
        var asOfUtc = DateTime.SpecifyKind(asOf, DateTimeKind.Utc);
        var row = await _context.VendorLedgerEntries
            .AsNoTracking()
            .Where(e => e.PostingDate <= asOfUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Debit = g.Sum(e => e.EntryType == LedgerEntryType.Debit ? e.Amount : 0m),
                Credit = g.Sum(e => e.EntryType == LedgerEntryType.Credit ? e.Amount : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null) return 0m;
        return Math.Round(row.Credit - row.Debit, 4);
    }
}
