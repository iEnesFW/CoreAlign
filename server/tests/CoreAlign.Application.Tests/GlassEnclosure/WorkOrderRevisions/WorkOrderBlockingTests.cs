using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.GlassEnclosure.WorkOrderRevisions;

public class WorkOrderBlockingTests
{
    private static GlassWorkOrder BuildWorkOrder() => new(
        projectId: Guid.NewGuid(),
        scheduledStartDate: DateTime.UtcNow.AddDays(1),
        scheduledEndDate: DateTime.UtcNow.AddDays(2),
        workloadM2: 10m);

    [Fact]
    public void WorkOrder_TransitionTo_with_outstanding_blocking_revision_throws()
    {
        var workOrder = BuildWorkOrder();
        workOrder.MarkBlockingRevision();

        var act = () => workOrder.TransitionTo(GlassWorkOrderStatus.Cutting);

        act.Should().Throw<WorkOrderBlockedByRevisionException>()
            .Which.WorkOrderId.Should().Be(workOrder.Id);
        workOrder.Status.Should().Be(GlassWorkOrderStatus.Pending);
    }

    [Fact]
    public void WorkOrder_ClearBlockingRevision_allows_transition()
    {
        var workOrder = BuildWorkOrder();
        workOrder.MarkBlockingRevision();
        workOrder.ClearBlockingRevision();

        var act = () => workOrder.TransitionTo(GlassWorkOrderStatus.Cutting);

        act.Should().NotThrow();
        workOrder.Status.Should().Be(GlassWorkOrderStatus.Cutting);
        workOrder.HasOutstandingBlockingRevision.Should().BeFalse();
    }

    [Fact]
    public void TransitionFromDefectiveToPending_withoutNewRevision_throws()
    {
        var workOrder = BuildWorkOrder();
        workOrder.IncrementRevisionCount();
        workOrder.TransitionTo(GlassWorkOrderStatus.Defective);

        var act = () => workOrder.TransitionTo(GlassWorkOrderStatus.Pending);

        act.Should().Throw<DefectiveExitRequiresRevisionException>()
            .Which.WorkOrderId.Should().Be(workOrder.Id);
        workOrder.Status.Should().Be(GlassWorkOrderStatus.Defective);
    }

    [Fact]
    public void TransitionFromDefectiveToInstalled_withoutNewRevision_succeeds()
    {
        var workOrder = BuildWorkOrder();
        workOrder.IncrementRevisionCount();
        workOrder.TransitionTo(GlassWorkOrderStatus.Defective);

        var act = () => workOrder.TransitionTo(GlassWorkOrderStatus.Installed);

        act.Should().NotThrow();
        workOrder.Status.Should().Be(GlassWorkOrderStatus.Installed);
    }

    [Fact]
    public void TransitionFromDefective_withNewRevision_succeeds()
    {
        var workOrder = BuildWorkOrder();
        workOrder.IncrementRevisionCount();
        workOrder.TransitionTo(GlassWorkOrderStatus.Defective);
        workOrder.IncrementRevisionCount();

        var act = () => workOrder.TransitionTo(GlassWorkOrderStatus.Pending);

        act.Should().NotThrow();
        workOrder.Status.Should().Be(GlassWorkOrderStatus.Pending);
    }
}
