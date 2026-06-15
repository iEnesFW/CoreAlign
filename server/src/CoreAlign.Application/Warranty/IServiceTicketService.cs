using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Warranty;

public interface IServiceTicketService
{
    Task<ServiceTicket> OpenAsync(
        Guid customerId,
        ServiceTicketType type,
        ServiceTicketPriority priority,
        string title,
        string descriptionMd,
        Guid? warrantyContractId,
        CancellationToken cancellationToken = default);

    Task AssignAsync(Guid ticketId, Guid userId, CancellationToken cancellationToken = default);
    Task StartWorkAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task ResolveAsync(Guid ticketId, string resolutionNotesMd, Guid? workOrderId, decimal? chargeableAmount, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid ticketId, string? reason, CancellationToken cancellationToken = default);
}
