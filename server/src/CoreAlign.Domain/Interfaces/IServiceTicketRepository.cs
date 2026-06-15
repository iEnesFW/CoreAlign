using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IServiceTicketRepository
{
    Task<ServiceTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceTicket>> ListAsync(
        ServiceTicketStatus? status,
        ServiceTicketType? type,
        ServiceTicketPriority? priority,
        Guid? customerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceTicket>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceTicket>> ListByWarrantyContractAsync(Guid warrantyContractId, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceTicket ticket, CancellationToken cancellationToken = default);
    void Update(ServiceTicket ticket);
}
