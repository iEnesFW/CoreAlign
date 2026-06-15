using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Events;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.WorkOrderRevisions;

public class WorkOrderRevisionServiceTests
{
    private readonly IGlassWorkOrderRepository _workOrders = Substitute.For<IGlassWorkOrderRepository>();
    private readonly IGlassWorkOrderRevisionRepository _revisions = Substitute.For<IGlassWorkOrderRevisionRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IAuditContext _auditContext = Substitute.For<IAuditContext>();

    public WorkOrderRevisionServiceTests()
    {
        _currentUser.UserIdOrThrow().Returns(Guid.NewGuid());
    }

    private WorkOrderRevisionService BuildSut() => new(_workOrders, _revisions, _currentUser, _auditContext);

    private static GlassWorkOrder BuildWorkOrder(decimal? snapshotTotal = 1000m, string? snapshotJson = "{\"baseline\":true}")
    {
        var workOrder = new GlassWorkOrder(
            projectId: Guid.NewGuid(),
            scheduledStartDate: DateTime.UtcNow.AddDays(1),
            scheduledEndDate: DateTime.UtcNow.AddDays(2),
            workloadM2: 12m);
        if (snapshotTotal.HasValue && snapshotJson is not null)
        {
            workOrder.CaptureBomSnapshot(snapshotJson, snapshotTotal.Value, cuttingPlan1DId: null, cuttingPlan2DId: null);
        }
        return workOrder;
    }

    [Fact]
    public async Task CreateRevisionAsync_throws_when_work_order_missing()
    {
        var missingId = Guid.NewGuid();
        _workOrders.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((GlassWorkOrder?)null);

        var sut = BuildSut();

        var act = async () => await sut.CreateRevisionAsync(missingId, "{}", 100m, "ad-hoc", default);

        await act.Should().ThrowAsync<GlassWorkOrderNotFoundException>();
    }

    [Fact]
    public async Task CreateRevisionAsync_below_5_percent_yields_silent_snapshot_and_updates_workorder()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();
        const string newSnapshot = "{\"v\":\"silent\"}";

        var decision = await sut.CreateRevisionAsync(workOrder.Id, newSnapshot, 1020m, "minor tweak", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.SilentSnapshot);
        decision.DeltaPercent.Should().BeLessThan(5m);
        workOrder.BomSnapshotJson.Should().Be(newSnapshot);
        workOrder.BomSnapshotTotal.Should().Be(1020m);
        await _revisions.Received(1).AddAsync(Arg.Any<GlassWorkOrderRevision>(), Arg.Any<CancellationToken>());
        _workOrders.Received(1).Update(workOrder);
    }

    [Fact]
    public async Task CreateRevisionAsync_between_5_and_10_percent_pending_approval_does_not_update_snapshot()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m, snapshotJson: "{\"baseline\":true}");
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();
        const string newSnapshot = "{\"v\":\"pending\"}";

        var decision = await sut.CreateRevisionAsync(workOrder.Id, newSnapshot, 1080m, "medium change", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.PendingApproval);
        decision.DeltaPercent.Should().BeInRange(5m, 10m);
        workOrder.BomSnapshotJson.Should().Be("{\"baseline\":true}");
        workOrder.BomSnapshotTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateRevisionAsync_above_10_percent_blocks_production()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m, snapshotJson: "{\"baseline\":true}");
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();
        const string newSnapshot = "{\"v\":\"blocked\"}";

        var decision = await sut.CreateRevisionAsync(workOrder.Id, newSnapshot, 1200m, "huge change", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.Blocked);
        decision.DeltaPercent.Should().BeGreaterThan(10m);
        workOrder.BomSnapshotJson.Should().Be("{\"baseline\":true}");
        workOrder.BomSnapshotTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateRevisionAsync_increments_workorder_revision_count()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();
        var initialCount = workOrder.RevisionCount;

        await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":1}", 1010m, "tiny", default);

        workOrder.RevisionCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public async Task CreateRevisionAsync_assigns_sequential_revision_numbers()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>())
            .Returns(0, 1, 2);

        var sut = BuildSut();

        var first = await sut.CreateRevisionAsync(workOrder.Id, "{\"r\":1}", 1005m, "r1", default);
        var second = await sut.CreateRevisionAsync(workOrder.Id, "{\"r\":2}", 1010m, "r2", default);
        var third = await sut.CreateRevisionAsync(workOrder.Id, "{\"r\":3}", 1015m, "r3", default);

        first!.RevisionNumber.Should().Be(1);
        second!.RevisionNumber.Should().Be(2);
        third!.RevisionNumber.Should().Be(3);
    }

    [Fact]
    public async Task ApproveRevisionAsync_marks_approved_and_captures_snapshot_on_workorder()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m, snapshotJson: "{\"baseline\":true}");
        const string newSnapshot = "{\"v\":\"approved\"}";
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1080m, deltaPercent = 8m });
        var revision = new GlassWorkOrderRevision(
            workOrder.Id,
            revisionNumber: 1,
            previousSnapshotJson: workOrder.BomSnapshotJson,
            newSnapshotJson: newSnapshot,
            deltaJson: deltaJson,
            deltaPercent: 8m,
            reason: "medium",
            status: WorkOrderRevisionStatus.PendingApproval,
            createdByUserId: Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        await sut.ApproveRevisionAsync(revision.Id, null, default);

        revision.Status.Should().Be(WorkOrderRevisionStatus.Approved);
        revision.ApprovedByUserId.Should().NotBeNull();
        revision.ApprovedAtUtc.Should().NotBeNull();
        workOrder.BomSnapshotJson.Should().Be(newSnapshot);
        workOrder.BomSnapshotTotal.Should().Be(1080m);
        _revisions.Received(1).Update(revision);
        _workOrders.Received(1).Update(workOrder);
        _auditContext.Received(1).CaptureCustom(
            revision.Id,
            "GlassWorkOrderRevision",
            "WorkOrderRevision.Approved",
            Arg.Any<string>());
    }

    [Fact]
    public async Task RejectRevisionAsync_records_rejection_reason()
    {
        const string rejectionReason = "Material price disputed";
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1200m, deltaPercent = 20m });
        var revision = new GlassWorkOrderRevision(
            workOrderId: Guid.NewGuid(),
            revisionNumber: 1,
            previousSnapshotJson: "{\"baseline\":true}",
            newSnapshotJson: "{\"v\":\"rejected\"}",
            deltaJson: deltaJson,
            deltaPercent: 20m,
            reason: "too big",
            status: WorkOrderRevisionStatus.Blocked,
            createdByUserId: Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);

        var sut = BuildSut();

        await sut.RejectRevisionAsync(revision.Id, rejectionReason, default);

        revision.Status.Should().Be(WorkOrderRevisionStatus.Rejected);
        revision.RejectionReason.Should().Be(rejectionReason);
        revision.ApprovedAtUtc.Should().NotBeNull();
        _revisions.Received(1).Update(revision);
        _auditContext.Received(1).CaptureCustom(
            revision.Id,
            "GlassWorkOrderRevision",
            "WorkOrderRevision.Rejected",
            Arg.Any<string>());
    }

    [Fact]
    public async Task CreateRevision_at_exact_5_percent_returns_PendingApproval()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"exact5\"}", 1050m, "boundary", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.PendingApproval);
        decision.DeltaPercent.Should().Be(5m);
    }

    [Fact]
    public async Task CreateRevision_at_exact_10_percent_returns_PendingApproval()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"exact10\"}", 1100m, "boundary", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.PendingApproval);
        decision.DeltaPercent.Should().Be(10m);
    }

    [Fact]
    public async Task CreateRevision_at_10_01_percent_returns_Blocked()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"over10\"}", 1100.10m, "boundary", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.Blocked);
        decision.DeltaPercent.Should().BeGreaterThan(10m);
    }

    [Fact]
    public async Task CreateRevision_at_4_99_percent_returns_SilentSnapshot()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"under5\"}", 1049.90m, "tiny", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.SilentSnapshot);
        decision.DeltaPercent.Should().BeLessThan(5m);
    }

    [Fact]
    public async Task CreateRevision_with_zero_previous_total_and_zero_new_total_returns_SilentSnapshot()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: null, snapshotJson: null);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"initial\"}", 0m, "initial", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.SilentSnapshot);
        decision.DeltaPercent.Should().Be(0m);
    }

    [Fact]
    public async Task CreateRevision_negative_delta_above_threshold_returns_Blocked()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 100m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"drop\"}", 80m, "negative", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.Blocked);
        decision.DeltaPercent.Should().Be(-20m);
    }

    [Fact]
    public async Task CreateRevision_with_identical_snapshot_returns_null()
    {
        const string snapshotJson = "{\"baseline\":true}";
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m, snapshotJson: snapshotJson);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, snapshotJson, 1000m, "no-op", default);

        decision.Should().BeNull();
        await _revisions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        _workOrders.DidNotReceive().Update(Arg.Any<GlassWorkOrder>());
    }

    [Fact]
    public async Task CreateRevision_sets_tenant_id_on_revision_entity()
    {
        var tenantId = Guid.NewGuid();
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        workOrder.TenantId = tenantId;
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        GlassWorkOrderRevision? captured = null;
        await _revisions.AddAsync(
            Arg.Do<GlassWorkOrderRevision>(r => captured = r),
            Arg.Any<CancellationToken>());

        var sut = BuildSut();

        await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"tenant\"}", 1010m, "tenant-check", default);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task CreateRevision_blocked_does_not_increment_revision_count()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);

        var sut = BuildSut();
        var initialCount = workOrder.RevisionCount;

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"blocked\"}", 1200m, "big", default);

        decision!.Status.Should().Be(WorkOrderRevisionStatus.Blocked);
        workOrder.RevisionCount.Should().Be(initialCount);
        workOrder.HasOutstandingBlockingRevision.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveRevision_with_pending_status_succeeds_and_updates_snapshot()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        const string newSnapshot = "{\"v\":\"pending-approved\"}";
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1075m, deltaPercent = 7.5m });
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, newSnapshot,
            deltaJson, 7.5m, "reason", WorkOrderRevisionStatus.PendingApproval, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        await sut.ApproveRevisionAsync(revision.Id, null, default);

        revision.Status.Should().Be(WorkOrderRevisionStatus.Approved);
        revision.OverrideReason.Should().BeNull();
        workOrder.BomSnapshotJson.Should().Be(newSnapshot);
        workOrder.BomSnapshotTotal.Should().Be(1075m);
    }

    [Fact]
    public async Task ApproveRevision_with_blocked_status_without_override_reason_throws()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1200m, deltaPercent = 20m });
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, "{\"v\":\"blocked-no-override\"}",
            deltaJson, 20m, "reason", WorkOrderRevisionStatus.Blocked, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        var act = async () => await sut.ApproveRevisionAsync(revision.Id, null, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        revision.Status.Should().Be(WorkOrderRevisionStatus.Blocked);
    }

    [Fact]
    public async Task ApproveRevision_with_missing_delta_payload_throws_invalid_operation()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, "{\"v\":\"missing-delta\"}",
            deltaJson: null, 7m, "reason", WorkOrderRevisionStatus.PendingApproval, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        var act = async () => await sut.ApproveRevisionAsync(revision.Id, null, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(ex => ex.Message.Contains("no delta payload"));
    }

    [Fact]
    public async Task ApproveRevision_with_malformed_delta_payload_throws_invalid_operation()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, "{\"v\":\"malformed-delta\"}",
            deltaJson: "not-json", 7m, "reason", WorkOrderRevisionStatus.PendingApproval, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        var act = async () => await sut.ApproveRevisionAsync(revision.Id, null, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(ex => ex.Message.Contains("malformed"));
    }

    [Fact]
    public async Task ApproveRevision_with_missing_newTotal_property_throws_invalid_operation()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, "{\"v\":\"missing-newTotal\"}",
            deltaJson: "{\"foo\":1}", 7m, "reason", WorkOrderRevisionStatus.PendingApproval, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        var act = async () => await sut.ApproveRevisionAsync(revision.Id, null, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(ex => ex.Message.Contains("missing newTotal"));
    }

    [Fact]
    public async Task ApproveRevision_with_blocked_status_and_override_reason_clears_blocking()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        workOrder.MarkBlockingRevision();
        const string newSnapshot = "{\"v\":\"override\"}";
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1300m, deltaPercent = 30m });
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, newSnapshot,
            deltaJson, 30m, "reason", WorkOrderRevisionStatus.Blocked, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        await sut.ApproveRevisionAsync(revision.Id, "Manager override: customer escalation", default);

        revision.Status.Should().Be(WorkOrderRevisionStatus.Approved);
        revision.OverrideReason.Should().Be("Manager override: customer escalation");
        workOrder.HasOutstandingBlockingRevision.Should().BeFalse();
        workOrder.BomSnapshotJson.Should().Be(newSnapshot);
        workOrder.BomSnapshotTotal.Should().Be(1300m);
        _auditContext.Received(1).CaptureCustom(
            revision.Id,
            "GlassWorkOrderRevision",
            "WorkOrderRevision.BlockOverridden",
            Arg.Any<string>());
    }

    [Fact]
    public async Task CreateRevision_with_cumulative_drift_above_threshold_upgrades_silent_to_pending()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);
        _revisions.GetCumulativeSignedDeltaSinceLastApprovalAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(12m);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"silent-cum\"}", 1040m, "drift-accumulator", default);

        decision.Should().NotBeNull();
        decision!.DeltaPercent.Should().BeLessThan(5m);
        decision.Status.Should().Be(WorkOrderRevisionStatus.PendingApproval);
        workOrder.BomSnapshotJson.Should().Be("{\"baseline\":true}");
        workOrder.BomSnapshotTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateRevision_with_cumulative_drift_under_threshold_remains_silent()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);
        _revisions.GetCumulativeSignedDeltaSinceLastApprovalAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(5m);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"silent-stable\"}", 1040m, "still-silent", default);

        decision.Should().NotBeNull();
        decision!.Status.Should().Be(WorkOrderRevisionStatus.SilentSnapshot);
        decision.DeltaPercent.Should().BeLessThan(5m);
        workOrder.BomSnapshotJson.Should().Be("{\"v\":\"silent-stable\"}");
        workOrder.BomSnapshotTotal.Should().Be(1040m);
    }

    [Fact]
    public async Task CreateRevision_cumulative_drift_signed_negative_blocks()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0);
        _revisions.GetCumulativeSignedDeltaSinceLastApprovalAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(-13m);

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"silent-drop\"}", 960m, "downward-drift", default);

        decision.Should().NotBeNull();
        decision!.DeltaPercent.Should().BeLessThan(0m);
        Math.Abs(decision.DeltaPercent).Should().BeLessThan(5m);
        decision.Status.Should().Be(WorkOrderRevisionStatus.PendingApproval);
        workOrder.BomSnapshotJson.Should().Be("{\"baseline\":true}");
        workOrder.BomSnapshotTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task CreateRevision_with_concurrent_insert_retries_then_throws_ConcurrentRevisionInsertException()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0, 0);

        var addCalls = 0;
        _revisions.When(r => r.AddAsync(Arg.Any<GlassWorkOrderRevision>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                addCalls++;
                throw new InvalidOperationException("duplicate key value violates unique constraint \"ix_work_order_revisions_workorder_revisionnumber\"");
            });

        var sut = BuildSut();

        var act = async () => await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"concurrent\"}", 1010m, "race", default);

        var assertion = await act.Should().ThrowAsync<ConcurrentRevisionInsertException>();
        assertion.Which.WorkOrderId.Should().Be(workOrder.Id);
        assertion.Which.AttemptedRevisionNumber.Should().Be(1);
        addCalls.Should().Be(2);
        _workOrders.DidNotReceive().Update(Arg.Any<GlassWorkOrder>());
    }

    [Fact]
    public async Task ApproveBlockedRevision_withOtherBlockedPresent_doesNotClearBlocking()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        workOrder.MarkBlockingRevision();
        const string newSnapshot = "{\"v\":\"override-other-present\"}";
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1300m, deltaPercent = 30m });
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, newSnapshot,
            deltaJson, 30m, "reason", WorkOrderRevisionStatus.Blocked, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.AnyOutstandingBlockingAsync(workOrder.Id, revision.Id, Arg.Any<CancellationToken>()).Returns(true);

        var sut = BuildSut();

        await sut.ApproveRevisionAsync(revision.Id, "Manager override: with other pending", default);

        revision.Status.Should().Be(WorkOrderRevisionStatus.Approved);
        workOrder.HasOutstandingBlockingRevision.Should().BeTrue();
    }

    [Fact]
    public async Task RejectPendingRevision_doesNotBumpWorkOrder()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        var workOrderUpdatedBefore = workOrder.UpdatedAtUtc;
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1075m, deltaPercent = 7.5m });
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, "{\"v\":\"pending-rejected\"}",
            deltaJson, 7.5m, "reason", WorkOrderRevisionStatus.PendingApproval, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        await sut.RejectRevisionAsync(revision.Id, "not approved", default);

        revision.Status.Should().Be(WorkOrderRevisionStatus.Rejected);
        workOrder.UpdatedAtUtc.Should().Be(workOrderUpdatedBefore);
        _workOrders.DidNotReceive().Update(Arg.Any<GlassWorkOrder>());
        await _revisions.DidNotReceiveWithAnyArgs().AnyOutstandingBlockingAsync(default, default, default);
    }

    [Fact]
    public async Task OverrideBlock_raisesBlockOverriddenEvent_notApprovedEvent()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        workOrder.MarkBlockingRevision();
        const string newSnapshot = "{\"v\":\"override-event\"}";
        var deltaJson = JsonSerializer.Serialize(new { previousTotal = 1000m, newTotal = 1300m, deltaPercent = 30m });
        var revision = new GlassWorkOrderRevision(
            workOrder.Id, 1, workOrder.BomSnapshotJson, newSnapshot,
            deltaJson, 30m, "reason", WorkOrderRevisionStatus.Blocked, Guid.NewGuid());

        _revisions.GetByIdAsync(revision.Id, Arg.Any<CancellationToken>()).Returns(revision);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);

        var sut = BuildSut();

        await sut.ApproveRevisionAsync(revision.Id, "Manager override: compliance trace", default);

        revision.DomainEvents.OfType<GlassWorkOrderRevisionBlockOverriddenEvent>().Should().HaveCount(1);
        revision.DomainEvents.OfType<GlassWorkOrderRevisionApprovedEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task CreateRevision_with_first_attempt_failure_succeeds_on_retry()
    {
        var workOrder = BuildWorkOrder(snapshotTotal: 1000m);
        _workOrders.GetByIdAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(workOrder);
        _revisions.GetMaxRevisionNumberAsync(workOrder.Id, Arg.Any<CancellationToken>()).Returns(0, 1);

        var addCalls = 0;
        _revisions.When(r => r.AddAsync(Arg.Any<GlassWorkOrderRevision>(), Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                addCalls++;
                if (addCalls == 1)
                {
                    throw new InvalidOperationException("duplicate key value violates unique constraint");
                }
            });

        var sut = BuildSut();

        var decision = await sut.CreateRevisionAsync(workOrder.Id, "{\"v\":\"retry-success\"}", 1010m, "retry", default);

        decision.Should().NotBeNull();
        decision!.RevisionNumber.Should().Be(2);
        addCalls.Should().Be(2);
        await _revisions.Received(2).AddAsync(Arg.Any<GlassWorkOrderRevision>(), Arg.Any<CancellationToken>());
        _workOrders.Received(1).Update(workOrder);
    }
}
