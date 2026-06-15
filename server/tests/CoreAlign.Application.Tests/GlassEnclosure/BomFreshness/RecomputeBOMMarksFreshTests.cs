using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Application.GlassEnclosure.WorkOrderRevisions;
using CoreAlign.Application.Stock.Availability;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.BomFreshness;

public class RecomputeBOMMarksFreshTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IBOMComposer _composer = Substitute.For<IBOMComposer>();
    private readonly IGlassProjectBOMLineRepository _lineRepo = Substitute.For<IGlassProjectBOMLineRepository>();
    private readonly IStockAvailabilityService _availabilityService = Substitute.For<IStockAvailabilityService>();
    private readonly IBomStaleSignal _bomStaleSignal = Substitute.For<IBomStaleSignal>();
    private readonly IGlassWorkOrderRepository _workOrderRepo = Substitute.For<IGlassWorkOrderRepository>();
    private readonly IWorkOrderRevisionService _workOrderRevisionService = Substitute.For<IWorkOrderRevisionService>();

    [Fact]
    public async Task Handler_calls_signal_fresh_after_composing_bom()
    {
        var project = new GlassProject(
            code: "PRJ-1",
            customerId: Guid.NewGuid(),
            projectName: "Recompute Fresh",
            createdByUserId: Guid.NewGuid());
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var composition = new BOMCompositionResult(
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
            Subtotal: 0m,
            MarginAmount: 0m,
            TaxAmount: 0m,
            GrandTotal: 0m,
            Currency: "TRY",
            Lines: Array.Empty<BOMLineDraft>());
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(composition);
        _availabilityService.CheckAsync(project.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockAvailabilityRow>());

        var handler = new RecomputeBOMCommandHandler(_projectRepo, _composer, _lineRepo, _availabilityService, _bomStaleSignal, _workOrderRepo, _workOrderRevisionService);

        await handler.Handle(new RecomputeBOMCommand(project.Id), default);

        await _bomStaleSignal.Received(1).SignalFreshAsync(project.Id, Arg.Any<CancellationToken>());
    }
}
