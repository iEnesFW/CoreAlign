using CoreAlign.Application.Catalog.Linker;
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

    public BomComposerProductLinkTests()
    {
        _settingsRepo.GetOrCreateForCurrentTenantAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new GlassEnclosureSettings(Guid.NewGuid()));
        _hardwareKitRepo.ListAsync(Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HardwareKit>());
    }

    private BOMComposer BuildSut() => new(
        _systemRepo, _profileItemRepo, _glassRepo, _colorRepo,
        _hardwareRepo, _hardwareKitRepo, _settingsRepo, _evaluator, _linker);

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
