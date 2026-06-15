using CoreAlign.Domain.Events;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Warranty.Subscribers;

/// <summary>
/// F3.2 refactor: previously subscribed to <c>GlassWorkOrderInstalledEvent</c>.
/// Now subscribes to <c>InstallationAcceptedEvent</c> so that warranty activation
/// only occurs after the on-site acceptance protocol completes. Renamed
/// conceptually to InstallationAcceptedWarrantyActivator; class kept for
/// backward-compat DI lookups.
/// </summary>
public sealed class InstallationAcceptedWarrantyActivator : INotificationHandler<InstallationAcceptedEvent>
{
    private readonly IWarrantyContractRepository _contracts;
    private readonly IWarrantyContractService _service;
    private readonly ILogger<InstallationAcceptedWarrantyActivator> _logger;

    public InstallationAcceptedWarrantyActivator(
        IWarrantyContractRepository contracts,
        IWarrantyContractService service,
        ILogger<InstallationAcceptedWarrantyActivator> logger)
    {
        _contracts = contracts;
        _service = service;
        _logger = logger;
    }

    public async Task Handle(InstallationAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var contract = await _contracts.GetByWorkOrderIdAsync(notification.WorkOrderId, cancellationToken);
        if (contract is null)
        {
            _logger.LogDebug(
                "Installation accepted for WorkOrder {WorkOrderId} but no warranty contract linked.",
                notification.WorkOrderId);
            return;
        }

        await _service.ActivateAsync(contract.Id, notification.AcceptedAtUtc, cancellationToken);
        _logger.LogInformation(
            "Warranty contract {ContractId} ({Number}) activated for tenant {TenantId} after installation acceptance {AcceptanceId}.",
            contract.Id, contract.Number, notification.TenantId, notification.AcceptanceId);
    }
}
