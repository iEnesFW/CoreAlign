using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ServiceTicketRepository : IServiceTicketRepository
{
    private readonly CoreAlignDbContext _context;
    public ServiceTicketRepository(CoreAlignDbContext context) => _context = context;

    public Task<ServiceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ServiceTickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ServiceTicket>> ListAsync(
        ServiceTicketStatus? status,
        ServiceTicketType? type,
        ServiceTicketPriority? priority,
        Guid? customerId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceTickets.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (type.HasValue) query = query.Where(t => t.Type == type.Value);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);
        if (customerId.HasValue) query = query.Where(t => t.CustomerId == customerId.Value);
        return await query.OrderByDescending(t => t.ReportedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceTicket>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.ServiceTickets
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.ReportedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceTicket>> ListByWarrantyContractAsync(Guid warrantyContractId, CancellationToken cancellationToken = default) =>
        await _context.ServiceTickets
            .AsNoTracking()
            .Where(t => t.WarrantyContractId == warrantyContractId)
            .OrderByDescending(t => t.ReportedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ServiceTicket ticket, CancellationToken cancellationToken = default) =>
        await _context.ServiceTickets.AddAsync(ticket, cancellationToken);

    public void Update(ServiceTicket ticket) => _context.ServiceTickets.Update(ticket);
}
