using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// The live cost preview endpoint must ALWAYS live-compose the current scene (never return stale
/// persisted lines) and derive totals from the same BomQuoteTotalsCalculator the quote/recompute
/// uses — so the on-screen price equals the real quote.
/// </summary>
public class GetBomPreviewQueryHandlerTests
{
    private readonly IGlassProjectRepository _projectRepo = Substitute.For<IGlassProjectRepository>();
    private readonly IBOMComposer _composer = Substitute.For<IBOMComposer>();

    [Fact]
    public async Task Handler_live_composes_and_returns_totals_from_the_shared_calculator()
    {
        var project = new GlassProject("PRJ-PREVIEW", Guid.NewGuid(), "Preview", Guid.NewGuid());
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        var composition = new BOMCompositionResult(
            TotalAreaM2: 4m, TotalPanels: 2, TotalWeightKg: 50m,
            ProfileCost: 200m, GlassCost: 0m, HardwareCost: 0m, LaborCost: 0m,
            WasteCost: 0m, TransportCost: 0m, ScaffoldingCost: 0m, CraneCost: 0m,
            Subtotal: 200m, MarginAmount: 0m, TaxAmount: 0m, GrandTotal: 0m,
            Currency: "TRY",
            Lines: new[]
            {
                new BOMLineDraft(GlassBOMLineKind.ProfileCut, Guid.NewGuid(), Guid.NewGuid(), false,
                    "Top profile", 2m, "m", 100m, "TRY", "run-1", 0),
            });
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(composition);

        var summary = await new GetBomPreviewQueryHandler(_projectRepo, _composer)
            .Handle(new GetBomPreviewQuery(project.Id), default);

        await _composer.Received(1).ComposeAsync(project, Arg.Any<CancellationToken>());
        summary.ProfileCost.Should().Be(200m);
        summary.Lines.Should().HaveCount(1);
        summary.Subtotal.Should().Be(200m);   // Σ lineCost = 2 × 100
        summary.TaxAmount.Should().Be(40m);    // 200 × 20% (BomQuoteTotalsCalculator)
        summary.GrandTotal.Should().Be(240m);  // 200 + 40
    }

    [Fact]
    public async Task Handler_throws_not_found_when_project_missing()
    {
        _projectRepo.GetByIdWithRunsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GlassProject?)null);

        var act = async () => await new GetBomPreviewQueryHandler(_projectRepo, _composer)
            .Handle(new GetBomPreviewQuery(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<GlassProjectNotFoundException>();
    }
}
