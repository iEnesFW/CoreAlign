using CoreAlign.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Installation.Subscribers;

public sealed class WorkOrderInstalledAcceptanceOpener : INotificationHandler<GlassWorkOrderInstalledEvent>
{
    private readonly IInstallationAcceptanceService _service;
    private readonly ILogger<WorkOrderInstalledAcceptanceOpener> _logger;

    public WorkOrderInstalledAcceptanceOpener(
        IInstallationAcceptanceService service,
        ILogger<WorkOrderInstalledAcceptanceOpener> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task Handle(GlassWorkOrderInstalledEvent notification, CancellationToken cancellationToken)
    {
        var acceptance = await _service.StartAsync(
            notification.WorkOrderId,
            inspectorUserId: Guid.Empty,
            cancellationToken);

        _logger.LogInformation(
            "Installation acceptance {AcceptanceId} auto-opened for WorkOrder {WorkOrderId} (tenant {TenantId}).",
            acceptance.Id, notification.WorkOrderId, notification.TenantId);
    }
}
