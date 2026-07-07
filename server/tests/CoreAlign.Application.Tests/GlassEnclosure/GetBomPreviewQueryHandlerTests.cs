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

        // Composition carries a 20% tax (40 on an after-margin base of 200); the handler recovers
        // that rate the same way it recovers margin, so the preview honours the tenant setting.
        var summary = await RunAsync(project, subtotal: 200m, marginAmount: 0m, taxAmount: 40m);

        await _composer.Received(1).ComposeAsync(project, Arg.Any<CancellationToken>());
        summary.ProfileCost.Should().Be(200m);
        summary.Lines.Should().HaveCount(1);
        summary.Subtotal.Should().Be(200m);   // Σ lineCost = 2 × 100
        summary.TaxAmount.Should().Be(40m);    // recovered 20% rate
        summary.GrandTotal.Should().Be(240m);  // 200 + 40
    }

    [Fact]
    public async Task Handler_honours_a_non_default_tenant_tax_rate_from_the_composition()
    {
        var project = new GlassProject("PRJ-PREVIEW", Guid.NewGuid(), "Preview", Guid.NewGuid());

        // A tenant configured 10% VAT → composition tax is 20 on an after-margin base of 200.
        var summary = await RunAsync(project, subtotal: 200m, marginAmount: 0m, taxAmount: 20m);

        summary.TaxAmount.Should().Be(20m);    // recovered 10% rate, NOT a hardcoded 20%
        summary.GrandTotal.Should().Be(220m);
    }

    private async Task<CoreAlign.Application.GlassEnclosure.DTOs.BOMSummaryDto> RunAsync(
        GlassProject project, decimal subtotal, decimal marginAmount, decimal taxAmount)
    {
        _projectRepo.GetByIdWithRunsAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        var composition = new BOMCompositionResult(
            TotalAreaM2: 4m, TotalPanels: 2, TotalWeightKg: 50m,
            ProfileCost: 200m, GlassCost: 0m, HardwareCost: 0m, LaborCost: 0m,
            WasteCost: 0m, TransportCost: 0m, ScaffoldingCost: 0m, CraneCost: 0m,
            Subtotal: subtotal, MarginAmount: marginAmount, TaxAmount: taxAmount, GrandTotal: 0m,
            Currency: "TRY",
            Lines: new[]
            {
                new BOMLineDraft(GlassBOMLineKind.ProfileCut, Guid.NewGuid(), Guid.NewGuid(), false,
                    "Top profile", 2m, "m", 100m, "TRY", "run-1", 0),
            });
        _composer.ComposeAsync(project, Arg.Any<CancellationToken>()).Returns(composition);
        return await new GetBomPreviewQueryHandler(_projectRepo, _composer)
            .Handle(new GetBomPreviewQuery(project.Id), default);
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
