using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class WarrantyContractRepository : IWarrantyContractRepository
{
    private readonly CoreAlignDbContext _context;
    public WarrantyContractRepository(CoreAlignDbContext context) => _context = context;

    public Task<WarrantyContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.WarrantyContracts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<WarrantyContract?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _context.WarrantyContracts.FirstOrDefaultAsync(c => c.OrderId == orderId, cancellationToken);

    public Task<WarrantyContract?> GetByWorkOrderIdAsync(Guid workOrderId, CancellationToken cancellationToken = default) =>
        _context.WarrantyContracts.FirstOrDefaultAsync(c => c.WorkOrderId == workOrderId, cancellationToken);

    public async Task<IReadOnlyList<WarrantyContract>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.WarrantyContracts
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarrantyContract>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await _context.WarrantyContracts
            .AsNoTracking()
            .Where(c => c.OrderId == orderId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarrantyContract>> ListAsync(
        WarrantyContractStatus? status,
        Guid? customerId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WarrantyContracts.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId.Value);
        return await query.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarrantyContract>> ListExpiringWithinDaysAsync(int days, CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddDays(days);
        return await _context.WarrantyContracts
            .AsNoTracking()
            .Where(c => c.Status == WarrantyContractStatus.Active && c.EndDate <= threshold)
            .OrderBy(c => c.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForNumberSequenceAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _context.WarrantyContracts
            .IgnoreQueryFilters()
            .CountAsync(c => c.TenantId == _context.CurrentTenantIdOrEmpty
                && c.CreatedAtUtc.Year == year, cancellationToken);
    }

    public async Task AddAsync(WarrantyContract contract, CancellationToken cancellationToken = default) =>
        await _context.WarrantyContracts.AddAsync(contract, cancellationToken);

    public void Update(WarrantyContract contract) => _context.WarrantyContracts.Update(contract);
}
