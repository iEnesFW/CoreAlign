using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class CustomerUserRepository : ICustomerUserRepository
{
    private readonly CoreAlignDbContext _context;
    public CustomerUserRepository(CoreAlignDbContext context) => _context = context;

    public Task<CustomerUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.CustomerUsers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CustomerUser?> GetByUserAndCustomerAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default) =>
        _context.CustomerUsers.FirstOrDefaultAsync(
            c => c.UserId == userId && c.CustomerId == customerId,
            cancellationToken);

    public async Task<IReadOnlyList<CustomerUser>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.CustomerUsers
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.InvitedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerUser>> ListByTenantAsync(CancellationToken cancellationToken = default) =>
        await _context.CustomerUsers
            .AsNoTracking()
            .OrderBy(c => c.InvitedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CustomerUser>> ListActiveByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        await _context.CustomerUsers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.TenantId == tenantId && c.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveOwnershipAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default) =>
        _context.CustomerUsers.AnyAsync(
            c => c.UserId == userId
                && c.CustomerId == customerId
                && c.Status == MembershipStatus.Active
                && c.MembershipRole == CustomerMembershipRole.CustomerOwner,
            cancellationToken);

    public Task<bool> AnyActiveForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        _context.CustomerUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                c => c.UserId == userId && c.TenantId == tenantId && c.Status == MembershipStatus.Active,
                cancellationToken);

    public Task AddAsync(CustomerUser entity, CancellationToken cancellationToken = default) =>
        _context.CustomerUsers.AddAsync(entity, cancellationToken).AsTask();

    public void Update(CustomerUser entity) => _context.CustomerUsers.Update(entity);
}

public class DealerAccountRepository : IDealerAccountRepository
{
    private readonly CoreAlignDbContext _context;
    public DealerAccountRepository(CoreAlignDbContext context) => _context = context;

    public Task<DealerAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.DealerAccounts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<DealerAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.DealerAccounts.FirstOrDefaultAsync(d => d.Code == code, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken = default) =>
        excludeId.HasValue
            ? _context.DealerAccounts.AnyAsync(d => d.Code == code && d.Id != excludeId.Value, cancellationToken)
            : _context.DealerAccounts.AnyAsync(d => d.Code == code, cancellationToken);

    public async Task<IReadOnlyList<DealerAccount>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.DealerAccounts
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DealerAccount>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var dealerIds = _context.DealerCustomerLinks
            .AsNoTracking()
            .Where(l => l.CustomerId == customerId && l.Status == DealerCustomerLinkStatus.Active)
            .Select(l => l.DealerAccountId);

        return await _context.DealerAccounts
            .AsNoTracking()
            .Where(d => dealerIds.Contains(d.Id))
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(DealerAccount entity, CancellationToken cancellationToken = default) =>
        _context.DealerAccounts.AddAsync(entity, cancellationToken).AsTask();

    public void Update(DealerAccount entity) => _context.DealerAccounts.Update(entity);
}

public class DealerUserRepository : IDealerUserRepository
{
    private readonly CoreAlignDbContext _context;
    public DealerUserRepository(CoreAlignDbContext context) => _context = context;

    public Task<DealerUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.DealerUsers.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<DealerUser?> GetByUserAndDealerAsync(Guid userId, Guid dealerAccountId, CancellationToken cancellationToken = default) =>
        _context.DealerUsers.FirstOrDefaultAsync(
            d => d.UserId == userId && d.DealerAccountId == dealerAccountId,
            cancellationToken);

    public async Task<IReadOnlyList<DealerUser>> ListByDealerAsync(Guid dealerAccountId, CancellationToken cancellationToken = default) =>
        await _context.DealerUsers
            .AsNoTracking()
            .Where(d => d.DealerAccountId == dealerAccountId)
            .OrderBy(d => d.InvitedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DealerUser>> ListActiveByUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        await _context.DealerUsers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.TenantId == tenantId && d.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveOwnershipAsync(Guid userId, Guid dealerAccountId, CancellationToken cancellationToken = default) =>
        _context.DealerUsers.AnyAsync(
            d => d.UserId == userId
                && d.DealerAccountId == dealerAccountId
                && d.Status == MembershipStatus.Active
                && d.MembershipRole == DealerMembershipRole.DealerOwner,
            cancellationToken);

    public Task<bool> AnyActiveForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        _context.DealerUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                d => d.UserId == userId && d.TenantId == tenantId && d.Status == MembershipStatus.Active,
                cancellationToken);

    public Task AddAsync(DealerUser entity, CancellationToken cancellationToken = default) =>
        _context.DealerUsers.AddAsync(entity, cancellationToken).AsTask();

    public void Update(DealerUser entity) => _context.DealerUsers.Update(entity);
}

public class DealerCustomerLinkRepository : IDealerCustomerLinkRepository
{
    private readonly CoreAlignDbContext _context;
    public DealerCustomerLinkRepository(CoreAlignDbContext context) => _context = context;

    public Task<DealerCustomerLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.DealerCustomerLinks.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public Task<DealerCustomerLink?> GetByDealerAndCustomerAsync(Guid dealerAccountId, Guid customerId, CancellationToken cancellationToken = default) =>
        _context.DealerCustomerLinks.FirstOrDefaultAsync(
            l => l.DealerAccountId == dealerAccountId && l.CustomerId == customerId,
            cancellationToken);

    public async Task<IReadOnlyList<DealerCustomerLink>> ListByDealerAsync(Guid dealerAccountId, CancellationToken cancellationToken = default) =>
        await _context.DealerCustomerLinks
            .AsNoTracking()
            .Where(l => l.DealerAccountId == dealerAccountId)
            .OrderBy(l => l.AssignedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DealerCustomerLink>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.DealerCustomerLinks
            .AsNoTracking()
            .Where(l => l.CustomerId == customerId)
            .OrderBy(l => l.AssignedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DealerCustomerLink>> ListByFilterAsync(Guid? dealerAccountId, Guid? customerId, CancellationToken cancellationToken = default)
    {
        var query = _context.DealerCustomerLinks.AsNoTracking().AsQueryable();
        if (dealerAccountId.HasValue) query = query.Where(l => l.DealerAccountId == dealerAccountId.Value);
        if (customerId.HasValue) query = query.Where(l => l.CustomerId == customerId.Value);
        return await query.OrderBy(l => l.AssignedAtUtc).ToListAsync(cancellationToken);
    }

    public Task AddAsync(DealerCustomerLink entity, CancellationToken cancellationToken = default) =>
        _context.DealerCustomerLinks.AddAsync(entity, cancellationToken).AsTask();

    public void Update(DealerCustomerLink entity) => _context.DealerCustomerLinks.Update(entity);
}

public class CustomerDealerProductVisibilityRepository : ICustomerDealerProductVisibilityRepository
{
    private readonly CoreAlignDbContext _context;
    public CustomerDealerProductVisibilityRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<CustomerDealerProductVisibility>> ListByLinkAsync(Guid dealerCustomerLinkId, CancellationToken cancellationToken = default) =>
        await _context.CustomerDealerProductVisibilities
            .Where(v => v.DealerCustomerLinkId == dealerCustomerLinkId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListVisibleProductIdsAsync(Guid dealerCustomerLinkId, CancellationToken cancellationToken = default) =>
        await _context.CustomerDealerProductVisibilities
            .AsNoTracking()
            .Where(v => v.DealerCustomerLinkId == dealerCustomerLinkId)
            .Select(v => v.ProductId)
            .ToListAsync(cancellationToken);

    public Task<bool> HasAnyForLinkAsync(Guid dealerCustomerLinkId, CancellationToken cancellationToken = default) =>
        _context.CustomerDealerProductVisibilities
            .AnyAsync(v => v.DealerCustomerLinkId == dealerCustomerLinkId, cancellationToken);

    public Task AddAsync(CustomerDealerProductVisibility entity, CancellationToken cancellationToken = default) =>
        _context.CustomerDealerProductVisibilities.AddAsync(entity, cancellationToken).AsTask();

    public Task RemoveRangeAsync(IEnumerable<CustomerDealerProductVisibility> entities, CancellationToken cancellationToken = default)
    {
        _context.CustomerDealerProductVisibilities.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task<CustomerDealerProductVisibility?> GetAsync(Guid dealerCustomerLinkId, Guid productId, CancellationToken cancellationToken = default) =>
        _context.CustomerDealerProductVisibilities
            .FirstOrDefaultAsync(v => v.DealerCustomerLinkId == dealerCustomerLinkId && v.ProductId == productId, cancellationToken);
}

public class DealerCommissionLedgerEntryRepository : IDealerCommissionLedgerRepository
{
    private readonly CoreAlignDbContext _context;
    public DealerCommissionLedgerEntryRepository(CoreAlignDbContext context) => _context = context;

    public Task<DealerCommissionLedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.DealerCommissionLedgerEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<DealerCommissionLedgerEntry?> GetByOrderAndShipmentAsync(
        Guid dealerAccountId,
        Guid orderId,
        Guid? shipmentId,
        CancellationToken cancellationToken = default) =>
        _context.DealerCommissionLedgerEntries.FirstOrDefaultAsync(
            e => e.DealerAccountId == dealerAccountId && e.OrderId == orderId && e.ShipmentId == shipmentId,
            cancellationToken);

    public Task<bool> ExistsForOrderAndShipmentAsync(
        Guid dealerAccountId,
        Guid orderId,
        Guid? shipmentId,
        CancellationToken cancellationToken = default) =>
        _context.DealerCommissionLedgerEntries.AnyAsync(
            e => e.DealerAccountId == dealerAccountId && e.OrderId == orderId && e.ShipmentId == shipmentId,
            cancellationToken);

    public async Task<(IReadOnlyList<DealerCommissionLedgerEntry> Items, int Total)> SearchAsync(
        Guid dealerAccountId,
        DealerCommissionStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DealerCommissionLedgerEntries
            .AsNoTracking()
            .Where(e => e.DealerAccountId == dealerAccountId);

        if (status.HasValue) query = query.Where(e => e.Status == status.Value);
        if (fromUtc.HasValue) query = query.Where(e => e.AccruedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(e => e.AccruedAtUtc <= toUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.AccruedAtUtc)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<DealerCommissionLedgerEntry>> ListForStatementAsync(
        Guid dealerAccountId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) =>
        await _context.DealerCommissionLedgerEntries
            .AsNoTracking()
            .Where(e => e.DealerAccountId == dealerAccountId
                && e.AccruedAtUtc >= fromUtc
                && e.AccruedAtUtc <= toUtc)
            .OrderBy(e => e.AccruedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<DealerCommissionSummary> GetSummaryAsync(
        Guid dealerAccountId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var yearStart = new DateTime(nowUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var rows = await _context.DealerCommissionLedgerEntries
            .AsNoTracking()
            .Where(e => e.DealerAccountId == dealerAccountId
                && e.Status != DealerCommissionStatus.Cancelled)
            .Select(e => new
            {
                e.AccruedAtUtc,
                e.PaidOutAtUtc,
                e.Status,
                e.CommissionAmount,
                e.Currency,
            })
            .ToListAsync(cancellationToken);

        decimal ytdAccrued = 0m, ytdPaid = 0m, monthAccrued = 0m, monthPaid = 0m, totalAccrued = 0m, totalPaid = 0m;
        foreach (var r in rows)
        {
            totalAccrued += r.CommissionAmount;
            if (r.Status == DealerCommissionStatus.Paid) totalPaid += r.CommissionAmount;
            if (r.AccruedAtUtc >= yearStart)
            {
                ytdAccrued += r.CommissionAmount;
                if (r.Status == DealerCommissionStatus.Paid) ytdPaid += r.CommissionAmount;
            }
            if (r.AccruedAtUtc >= monthStart)
            {
                monthAccrued += r.CommissionAmount;
                if (r.Status == DealerCommissionStatus.Paid) monthPaid += r.CommissionAmount;
            }
        }

        var currency = rows
            .GroupBy(r => r.Currency)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "TRY";

        return new DealerCommissionSummary(
            YtdAccrued: Math.Round(ytdAccrued, 4),
            YtdPaid: Math.Round(ytdPaid, 4),
            ThisMonthAccrued: Math.Round(monthAccrued, 4),
            ThisMonthPaid: Math.Round(monthPaid, 4),
            LifetimeAccrued: Math.Round(totalAccrued, 4),
            LifetimePaid: Math.Round(totalPaid, 4),
            Currency: currency);
    }

    public Task AddAsync(DealerCommissionLedgerEntry entry, CancellationToken cancellationToken = default) =>
        _context.DealerCommissionLedgerEntries.AddAsync(entry, cancellationToken).AsTask();

    public void Update(DealerCommissionLedgerEntry entry) => _context.DealerCommissionLedgerEntries.Update(entry);
}
