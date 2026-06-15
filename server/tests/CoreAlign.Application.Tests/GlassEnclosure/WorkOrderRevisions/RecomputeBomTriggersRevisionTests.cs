using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.WorkOrderRevisions;

public class RecomputeBomTriggersRevisionTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IBOMComposer _composer = Substitute.For<IBOMComposer>();
    private readonly IGlassProjectBOMLineRepository _lineRepo = Substitute.For<IGlassProjectBOMLineRepository>();
    private readonly IStockAvailabilityService _availabilityService = Substitute.For<IStockAvailabilityService>();
    private readonly IBomStaleSignal _bomStaleSignal = Substitute.For<IBomStaleSignal>();
    private readonly IGlassWorkOrderRepository _workOrderRepo = Substitute.For<IGlassWorkOrderRepository>();
    private readonly IWorkOrderRevisionService _revisionService = Substitute.For<IWorkOrderRevisionService>();

    private static BOMCompositionResult BuildComposition(decimal grandTotal = 1000m) => new(
        TotalAreaM2: 0m,
        TotalPanels: 0,
        TotalWeightKg: 0m,
        ProfileCost: 0m,
        GlassCost: 0m,
        HardwareCost: 0m,
        LaborCost: 0m,
        WasteCost: 0m,
        TransportCost: 0m,
        ScaffoldingCost: 0m,
        CraneCost: 0m,
        Subtotal: grandTotal,
        MarginAmount: 0m,
        TaxAmount: 0m,
        GrandTotal: grandTotal,
        Currency: "TRY",
        Lines: Array.Empty<BOMLineDraft>());

    private static GlassProject BuildProject() => new(
        code: "PRJ-REV",
        customerId: Guid.NewGuid(),
        projectName: "Revision Trigger",
        createdByUserId: Guid.NewGuid());

    private RecomputeBOMCommandHandler BuildHandler() => new(
        _projectRepo,
        _composer,
        _lineRepo,
        _availabilityService,
        _bomStaleSignal,
        _workOrderRepo,
        _revisionService);

    [Fact]
    public async Task Handler_does_not_trigger_revision_when_no_released_work_order_exists()
    {
        var project = BuildProject();
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(BuildComposition());
        _availabilityService.CheckAsync(project.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());
        _workOrderRepo.ListReleasableByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GlassWorkOrder>());

        var handler = BuildHandler();

        await handler.Handle(new RecomputeBOMCommand(project.Id), default);

        await _revisionService.DidNotReceiveWithAnyArgs().CreateRevisionAsync(
            default, default!, default, default!, default);
    }

    [Fact]
    public async Task Handler_does_not_trigger_revision_when_only_pending_work_orders_exist()
    {
        var project = BuildProject();
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(BuildComposition());
        _availabilityService.CheckAsync(project.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        _workOrderRepo.ListReleasableByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GlassWorkOrder>());

        var handler = BuildHandler();

        await handler.Handle(new RecomputeBOMCommand(project.Id), default);

        await _revisionService.DidNotReceiveWithAnyArgs().CreateRevisionAsync(
            default, default!, default, default!, default);
    }

    [Fact]
    public async Task Handler_triggers_revision_when_released_work_order_with_snapshot_exists()
    {
        var project = BuildProject();
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(BuildComposition(1200m));
        _availabilityService.CheckAsync(project.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        var releasedWorkOrder = new GlassWorkOrder(
            project.Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            workloadM2: 10m);
        releasedWorkOrder.CaptureBomSnapshot("{\"baseline\":true}", 1000m, null, null);
        releasedWorkOrder.TransitionTo(GlassWorkOrderStatus.Cutting);

        _workOrderRepo.ListReleasableByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { releasedWorkOrder });

        var handler = BuildHandler();

        await handler.Handle(new RecomputeBOMCommand(project.Id), default);

        await _revisionService.Received(1).CreateRevisionAsync(
            releasedWorkOrder.Id,
            Arg.Any<string>(),
            1200m,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecomputeBOM_with_pending_status_workOrder_with_snapshot_triggers_revision()
    {
        var project = BuildProject();
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(BuildComposition(1250m));
        _availabilityService.CheckAsync(project.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        var pendingWithSnapshot = new GlassWorkOrder(
            project.Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            workloadM2: 10m);
        pendingWithSnapshot.CaptureBomSnapshot("{\"baseline\":true}", 1000m, null, null);

        pendingWithSnapshot.Status.Should().Be(GlassWorkOrderStatus.Pending);
        _workOrderRepo.ListReleasableByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { pendingWithSnapshot });

        var handler = BuildHandler();

        await handler.Handle(new RecomputeBOMCommand(project.Id), default);

        await _revisionService.Received(1).CreateRevisionAsync(
            pendingWithSnapshot.Id,
            Arg.Any<string>(),
            1250m,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecomputeBOM_with_multiple_released_workOrders_triggers_revision_for_each()
    {
        var project = BuildProject();
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(BuildComposition(1500m));
        _availabilityService.CheckAsync(project.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        var wo1 = new GlassWorkOrder(project.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), workloadM2: 10m);
        wo1.CaptureBomSnapshot("{\"baseline\":1}", 1000m, null, null);
        wo1.TransitionTo(GlassWorkOrderStatus.Cutting);

        var wo2 = new GlassWorkOrder(project.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), workloadM2: 12m);
        wo2.CaptureBomSnapshot("{\"baseline\":2}", 1100m, null, null);
        wo2.TransitionTo(GlassWorkOrderStatus.Assembling);

        var wo3 = new GlassWorkOrder(project.Id, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), workloadM2: 14m);
        wo3.CaptureBomSnapshot("{\"baseline\":3}", 1200m, null, null);
        wo3.TransitionTo(GlassWorkOrderStatus.Ready);

        _workOrderRepo.ListReleasableByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { wo1, wo2, wo3 });

        var handler = BuildHandler();

        await handler.Handle(new RecomputeBOMCommand(project.Id), default);

        await _revisionService.Received(1).CreateRevisionAsync(wo1.Id, Arg.Any<string>(), 1500m, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _revisionService.Received(1).CreateRevisionAsync(wo2.Id, Arg.Any<string>(), 1500m, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _revisionService.Received(1).CreateRevisionAsync(wo3.Id, Arg.Any<string>(), 1500m, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _revisionService.Received(3).CreateRevisionAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecomputeBOM_with_no_snapshot_workOrder_does_not_trigger_revision()
    {
        var project = BuildProject();
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(BuildComposition(1500m));
        _availabilityService.CheckAsync(project.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        var noSnapshot = new GlassWorkOrder(
            project.Id,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            workloadM2: 10m);
        noSnapshot.TransitionTo(GlassWorkOrderStatus.Cutting);

        _workOrderRepo.ListReleasableByProjectAsync(project.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GlassWorkOrder>());

        var handler = BuildHandler();

        await handler.Handle(new RecomputeBOMCommand(project.Id), default);

        await _revisionService.DidNotReceiveWithAnyArgs().CreateRevisionAsync(
            default, default!, default, default!, default);
    }
}
