using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Application.Fx;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.Bom;

public class ArcPricingTransparencyTests
{
    private const decimal GlassPricePerM2 = 100m;
    private const decimal PanelAreaM2 = 2.2m;

    private readonly IProfileSystemRepository _systemRepo = Substitute.For<IProfileSystemRepository>();
    private readonly IProfileItemRepository _profileItemRepo = Substitute.For<IProfileItemRepository>();
    private readonly IGlassTypeRepository _glassRepo = Substitute.For<IGlassTypeRepository>();
    private readonly IColorOptionRepository _colorRepo = Substitute.For<IColorOptionRepository>();
    private readonly IHardwareItemRepository _hardwareRepo = Substitute.For<IHardwareItemRepository>();
    private readonly IHardwareKitRepository _hardwareKitRepo = Substitute.For<IHardwareKitRepository>();
    private readonly IGlassEnclosureSettingsRepository _settingsRepo = Substitute.For<IGlassEnclosureSettingsRepository>();
    private readonly IExpressionEvaluator _evaluator = Substitute.For<IExpressionEvaluator>();
    private readonly ICatalogProductLinker _linker = Substitute.For<ICatalogProductLinker>();
    private readonly IFxRateProvider _fx = Substitute.For<IFxRateProvider>();

    public ArcPricingTransparencyTests()
    {
        _hardwareKitRepo.ListAsync(Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HardwareKit>());
        _fx.ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<decimal>());
    }

    private BOMComposer BuildSut() => new(
        _systemRepo, _profileItemRepo, _glassRepo, _colorRepo,
        _hardwareRepo, _hardwareKitRepo, _settingsRepo, _evaluator, _linker, _fx);

    [Fact]
    public async Task Bent_arc_run_prices_glass_at_catalogue_rate_and_names_the_premium_on_its_own_line()
    {
        var project = BuildScenario(bentGlassCostFactor: 2.75m, curved: true, bentGlass: true);

        var result = await BuildSut().ComposeAsync(project);

        var glassLines = result.Lines.Where(l => l.Kind == GlassBOMLineKind.GlassPiece).ToList();
        glassLines.Should().HaveCount(2);

        var baseLine = glassLines.Single(l => !l.Description.Contains("Bombeli"));
        baseLine.UnitCost.Should().Be(GlassPricePerM2);

        var premiumLine = glassLines.Single(l => l.Description.Contains("Bombeli"));
        // The override-matching key must not carry the factor — see the composer's WHY note.
        premiumLine.Description.Should().NotContain("2.75").And.NotContain("2,75");
        premiumLine.UnitCost.Should().Be(GlassPricePerM2 * 1.75m);
        premiumLine.Quantity.Should().Be(baseLine.Quantity);
        premiumLine.IsService.Should().BeFalse();
        premiumLine.ProductId.Should().Be(baseLine.ProductId);
    }

    [Fact]
    public async Task Splitting_the_premium_out_does_not_change_what_the_project_costs()
    {
        var project = BuildScenario(bentGlassCostFactor: 2.75m, curved: true, bentGlass: true);

        var result = await BuildSut().ComposeAsync(project);

        // The old composer folded the factor into the glass rate: area x price x factor.
        result.GlassCost.Should().Be(PanelAreaM2 * GlassPricePerM2 * 2.75m);
    }

    [Fact]
    public async Task Factor_of_one_charges_no_premium_and_emits_no_surcharge_line()
    {
        var project = BuildScenario(bentGlassCostFactor: 1m, curved: true, bentGlass: true);

        var result = await BuildSut().ComposeAsync(project);

        result.Lines.Should().ContainSingle(l => l.Kind == GlassBOMLineKind.GlassPiece)
            .Which.UnitCost.Should().Be(GlassPricePerM2);
        result.GlassCost.Should().Be(PanelAreaM2 * GlassPricePerM2);
    }

    [Fact]
    public async Task Straight_run_never_pays_the_bent_glass_premium()
    {
        var project = BuildScenario(bentGlassCostFactor: 2.75m, curved: false, bentGlass: true);

        var result = await BuildSut().ComposeAsync(project);

        result.Lines.Should().ContainSingle(l => l.Kind == GlassBOMLineKind.GlassPiece)
            .Which.UnitCost.Should().Be(GlassPricePerM2);
        result.GlassCost.Should().Be(PanelAreaM2 * GlassPricePerM2);
    }

    [Fact]
    public async Task Arc_run_with_flat_segmented_panels_pays_no_glass_premium()
    {
        var project = BuildScenario(bentGlassCostFactor: 2.75m, curved: true, bentGlass: false);

        var result = await BuildSut().ComposeAsync(project);

        result.Lines.Should().ContainSingle(l => l.Kind == GlassBOMLineKind.GlassPiece)
            .Which.UnitCost.Should().Be(GlassPricePerM2);
    }

    private GlassProject BuildScenario(decimal bentGlassCostFactor, bool curved, bool bentGlass)
    {
        var settings = new GlassEnclosureSettings(Guid.NewGuid());
        settings.UpdateCore(
            defaultStockBarLengthMm: 6000,
            defaultJumboGlassWidthMm: 3210,
            defaultJumboGlassHeightMm: 2250,
            sawKerfMm: 5m,
            glassKerfMm: 3m,
            guillotineRequired: false,
            defaultWastePercent: 0m,
            laborCostPerM2: 0m,
            defaultMarginPercent: 0m,
            bendRailFeePerM: 150m,
            bentGlassCostFactor: bentGlassCostFactor);
        _settingsRepo.GetOrCreateForCurrentTenantAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var systemId = Guid.NewGuid();
        var glass = new GlassType(
            code: "G-T6",
            name: "Tempered 6",
            thicknessMm: 6,
            structure: GlassStructure.Tempered,
            pricePerM2: GlassPricePerM2,
            weightKgPerM2: 15m,
            allowablePressurePa: 1000m,
            maxPanelAreaM2: 4m,
            uValue: 1.4m,
            soundDb: 35m);

        var project = new GlassProject(
            code: "PRJ-ARC",
            customerId: Guid.NewGuid(),
            projectName: "Arc pricing",
            createdByUserId: Guid.NewGuid());

        var run = new GlassProjectRun(
            projectId: project.Id,
            orderIndex: 0,
            label: "Run-A",
            lengthMm: 2000,
            heightMm: 2200,
            profileSystemId: systemId);

        if (curved)
        {
            run.UpdateGeometry3D(z: 0, tiltDeg: 0m, arcRadiusMm: 3000, arcSweepDeg: 40m, arcGlassBent: bentGlass);
        }

        run.AddPanel(new GlassProjectPanel(
            runId: run.Id,
            panelIndex: 0,
            widthMm: 1000,
            openingType: GlassOpeningType.Fixed,
            glassTypeId: glass.Id));

        project.AddRun(run);

        _systemRepo.GetWithItemsAsync(systemId, Arg.Any<CancellationToken>()).Returns((ProfileSystem?)null);
        _glassRepo.GetByIdAsync(glass.Id, Arg.Any<CancellationToken>()).Returns(glass);
        _linker.EnsureLinkedAsync(glass, CatalogItemKind.Glass, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(glass.Id, Guid.NewGuid(), "GE-GLASS", false, false));

        return project;
    }
}
