using CoreAlign.Application.Installation;
using CoreAlign.Application.Installation.Subscribers;
using CoreAlign.Application.Installation.Templates;
using CoreAlign.Domain.Entities.Installation;
using CoreAlign.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Installation;

public class WorkOrderInstalledAcceptanceOpenerTests
{
    private readonly IInstallationAcceptanceService _service = Substitute.For<IInstallationAcceptanceService>();
    private readonly WorkOrderInstalledAcceptanceOpener _sut;

    public WorkOrderInstalledAcceptanceOpenerTests()
    {
        _sut = new WorkOrderInstalledAcceptanceOpener(
            _service,
            NullLogger<WorkOrderInstalledAcceptanceOpener>.Instance);
    }

    [Fact]
    public async Task Handle_opens_installation_acceptance_for_installed_work_order()
    {
        var workOrderId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var notification = new GlassWorkOrderInstalledEvent(
            TenantId: Guid.NewGuid(),
            WorkOrderId: workOrderId,
            ProjectId: projectId,
            InstalledAtUtc: DateTime.UtcNow,
            OccurredAtUtc: DateTime.UtcNow);

        var acceptance = new InstallationAcceptance(
            workOrderId: workOrderId,
            projectId: projectId,
            customerId: Guid.NewGuid(),
            inspectorUserId: Guid.Empty,
            initialChecklistJson: StandardChecklist.BuildInitialChecklistJson());

        _service.StartAsync(workOrderId, Guid.Empty, Arg.Any<CancellationToken>()).Returns(acceptance);

        await _sut.Handle(notification, CancellationToken.None);

        await _service.Received(1).StartAsync(workOrderId, Guid.Empty, Arg.Any<CancellationToken>());
    }
}
