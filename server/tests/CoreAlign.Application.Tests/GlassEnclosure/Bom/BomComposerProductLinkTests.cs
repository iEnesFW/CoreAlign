using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Application.Fx;
using CoreAlign.Application.GlassEnclosure.Services;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.Bom;

public class BomComposerProductLinkTests
{
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

    public BomComposerProductLinkTests()
    {
        _settingsRepo.GetOrCreateForCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new GlassEnclosureSettings(Guid.NewGuid()));
        _hardwareKitRepo.ListAsync(Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HardwareKit>());
        // Identity conversion (single-currency scenarios stay byte-identical).
        _fx.ConvertAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<decimal>());
    }

    private BOMComposer BuildSut() => new(
        _systemRepo, _profileItemRepo, _glassRepo, _colorRepo,
        _hardwareRepo, _hardwareKitRepo, _settingsRepo, _evaluator, _linker, _fx);

    [Fact]
    public async Task ComposeAsync_sets_glass_product_id_from_catalog_linkage()
    {
        var (project, glass, expectedProductId) = BuildSingleGlassPanelScenario(catalogPreLinked: true);
        _linker.EnsureLinkedAsync(glass, CatalogItemKind.Glass, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(glass.Id, expectedProductId, "GE-GLASS", false, false));

        var result = await BuildSut().ComposeAsync(project);

        var glassLine = result.Lines.Should().ContainSingle(l => l.Kind == GlassBOMLineKind.GlassPiece).Subject;
        glassLine.ProductId.Should().Be(expectedProductId);
        glassLine.IsService.Should().BeFalse();
        glassLine.RefId.Should().Be(glass.Id);
    }

    [Fact]
    public async Task ComposeAsync_invokes_catalog_product_linker_for_glass_panel()
    {
        var (project, glass, productId) = BuildSingleGlassPanelScenario(catalogPreLinked: false);
        _linker.EnsureLinkedAsync(glass, CatalogItemKind.Glass, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(glass.Id, productId, "GE-GLASS-NEW", true, true));

        var result = await BuildSut().ComposeAsync(project);

        await _linker.Received(1).EnsureLinkedAsync(glass, CatalogItemKind.Glass, Arg.Any<CancellationToken>());
        result.Lines.Should().Contain(l => l.Kind == GlassBOMLineKind.GlassPiece && l.ProductId == productId);
    }

    [Fact]
    public async Task ComposeAsync_emits_hardware_piece_line_for_panel_hardware()
    {
        var (project, glass, _) = BuildSingleGlassPanelScenario(catalogPreLinked: true);
        _linker.EnsureLinkedAsync(glass, CatalogItemKind.Glass, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(glass.Id, Guid.NewGuid(), "GE-GLASS", false, false));

        var hardwareProductId = Guid.NewGuid();
        var hardware = new HardwareItem(
            code: "HW-HINGE",
            name: "Hinge 90",
            category: HardwareCategoryKind.Other,
            brandId: Guid.NewGuid(),
            unit: "Piece",
            unitPrice: 25m);
        _hardwareRepo.GetByIdAsync(hardware.Id, Arg.Any<CancellationToken>()).Returns(hardware);
        _linker.EnsureLinkedAsync(hardware, CatalogItemKind.Hardware, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(hardware.Id, hardwareProductId, "GE-HW", false, false));

        var panel = project.Runs.Single().Panels.Single();
        panel.ReplaceHardware(new[] { (hardware.Id, 3m) });

        var result = await BuildSut().ComposeAsync(project);

        var hardwareLine = result.Lines.Should()
            .ContainSingle(l => l.Kind == GlassBOMLineKind.HardwarePiece && l.RefId == hardware.Id).Subject;
        hardwareLine.ProductId.Should().Be(hardwareProductId);
        hardwareLine.Quantity.Should().Be(3m);
        hardwareLine.UnitCost.Should().Be(25m);
        hardwareLine.IsService.Should().BeFalse();
        hardwareLine.Source.Should().Be(panel.Id.ToString());
        result.HardwareCost.Should().Be(75m);
    }

    [Fact]
    public async Task ComposeAsync_skips_panel_hardware_when_catalog_item_missing()
    {
        var (project, glass, _) = BuildSingleGlassPanelScenario(catalogPreLinked: true);
        _linker.EnsureLinkedAsync(glass, CatalogItemKind.Glass, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(glass.Id, Guid.NewGuid(), "GE-GLASS", false, false));

        // Panel references a hardware id whose catalog item no longer resolves (GetByIdAsync → null).
        var missingHardwareId = Guid.NewGuid();
        _hardwareRepo.GetByIdAsync(missingHardwareId, Arg.Any<CancellationToken>()).Returns((HardwareItem?)null);

        var panel = project.Runs.Single().Panels.Single();
        panel.ReplaceHardware(new[] { (missingHardwareId, 3m) });

        var result = await BuildSut().ComposeAsync(project);

        result.Lines.Should().NotContain(l => l.Kind == GlassBOMLineKind.HardwarePiece);
        result.HardwareCost.Should().Be(0m);
    }

    private (GlassProject Project, GlassType Glass, Guid ProductId) BuildSingleGlassPanelScenario(bool catalogPreLinked)
    {
        var productId = Guid.NewGuid();
        var systemId = Guid.NewGuid();
        var glass = new GlassType(
            code: "G-T6",
            name: "Tempered 6",
            thicknessMm: 6,
            structure: GlassStructure.Tempered,
            pricePerM2: 100m,
            weightKgPerM2: 15m,
            allowablePressurePa: 1000m,
            maxPanelAreaM2: 4m,
            uValue: 1.4m,
            soundDb: 35m);
        if (catalogPreLinked)
        {
            glass.LinkedProductId = productId;
        }

        var project = new GlassProject(
            code: "PRJ-1",
            customerId: Guid.NewGuid(),
            projectName: "Composer Smoke",
            createdByUserId: Guid.NewGuid());

        var run = new GlassProjectRun(
            projectId: project.Id,
            orderIndex: 0,
            label: "Run-A",
            lengthMm: 2000,
            heightMm: 2200,
            profileSystemId: systemId);

        run.AddPanel(new GlassProjectPanel(
            runId: run.Id,
            panelIndex: 0,
            widthMm: 1000,
            openingType: GlassOpeningType.Fixed,
            glassTypeId: glass.Id));

        project.AddRun(run);

        _systemRepo.GetWithItemsAsync(systemId, Arg.Any<CancellationToken>())
            .Returns((ProfileSystem?)null);
        _glassRepo.GetByIdAsync(glass.Id, Arg.Any<CancellationToken>()).Returns(glass);

        return (project, glass, productId);
    }
}
