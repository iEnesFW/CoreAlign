using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Warranty;

public class ServiceTicketService : IServiceTicketService
{
    private readonly IServiceTicketRepository _tickets;
    private readonly IWarrantyContractRepository _contracts;

    public ServiceTicketService(
        IServiceTicketRepository tickets,
        IWarrantyContractRepository contracts)
    {
        _tickets = tickets;
        _contracts = contracts;
    }

    public async Task<ServiceTicket> OpenAsync(
        Guid customerId,
        ServiceTicketType type,
        ServiceTicketPriority priority,
        string title,
        string descriptionMd,
        Guid? warrantyContractId,
        CancellationToken cancellationToken = default)
    {
        var isUnderWarranty = false;
        if (warrantyContractId.HasValue)
        {
            var contract = await _contracts.GetByIdAsync(warrantyContractId.Value, cancellationToken);
            isUnderWarranty = contract is not null && contract.IsValidAtDate(DateTime.UtcNow);
        }

        var ticket = new ServiceTicket(
            customerId,
            type,
            priority,
            title,
            descriptionMd,
            isUnderWarranty,
            warrantyContractId);

        await _tickets.AddAsync(ticket, cancellationToken);
        return ticket;
    }

    public async Task AssignAsync(Guid ticketId, Guid userId, CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service ticket {ticketId} not found.");
        ticket.Assign(userId);
        _tickets.Update(ticket);
    }

    public async Task StartWorkAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service ticket {ticketId} not found.");
        ticket.StartWork();
        _tickets.Update(ticket);
    }

    public async Task ResolveAsync(Guid ticketId, string resolutionNotesMd, Guid? workOrderId, decimal? chargeableAmount, CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service ticket {ticketId} not found.");
        ticket.Resolve(resolutionNotesMd, workOrderId, chargeableAmount);
        _tickets.Update(ticket);
    }

    public async Task CancelAsync(Guid ticketId, string? reason, CancellationToken cancellationToken = default)
    {
        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Service ticket {ticketId} not found.");
        ticket.Cancel(reason);
        _tickets.Update(ticket);
    }
}
