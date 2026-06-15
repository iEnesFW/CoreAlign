using CoreAlign.Application.B2B;
using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Application.GlassEnclosure.Bom;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure.Bom;

public class BomLineBackfillHandlerTests
{
    private readonly IGlassProjectBOMLineRepository _bomLines = Substitute.For<IGlassProjectBOMLineRepository>();
    private readonly IGlassTypeRepository _glassTypes = Substitute.For<IGlassTypeRepository>();
    private readonly IHardwareItemRepository _hardware = Substitute.For<IHardwareItemRepository>();
    private readonly IProfileItemRepository _profiles = Substitute.For<IProfileItemRepository>();
    private readonly ICatalogProductLinker _linker = Substitute.For<ICatalogProductLinker>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();

    public BomLineBackfillHandlerTests()
    {
        _currentUser.UserIdOrThrow().Returns(Guid.NewGuid());
    }

    private BomLineBackfillHandler BuildSut() =>
        new(_bomLines, _glassTypes, _hardware, _profiles, _linker, _currentUser);

    [Fact]
    public async Task Handle_returns_zero_counts_when_repository_is_empty()
    {
        _bomLines.ListUnlinkedAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GlassProjectBOMLine>());

        var result = await BuildSut().Handle(new BomLineBackfillCommand(), default);

        result.TotalScanned.Should().Be(0);
        result.Linked.Should().Be(0);
        result.AlreadyLinked.Should().Be(0);
        result.CouldNotLink.Should().Be(0);
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_links_glass_hardware_and_profile_lines_when_catalog_items_are_already_linked()
    {
        var glassType = BuildGlassType(linked: true);
        var hardware = BuildHardware(linked: true);
        var profile = BuildProfile(linked: true);

        var glassLine = new GlassProjectBOMLine(
            Guid.NewGuid(), GlassBOMLineKind.GlassPiece, "Glass", 1m, "m²", 100m, "TRY",
            refId: glassType.Id);
        var hardwareLine = new GlassProjectBOMLine(
            Guid.NewGuid(), GlassBOMLineKind.HardwarePiece, "Hardware", 2m, "Piece", 5m, "TRY",
            refId: hardware.Id);
        var profileLine = new GlassProjectBOMLine(
            Guid.NewGuid(), GlassBOMLineKind.ProfileCut, "Profile", 3m, "m", 7m, "TRY",
            refId: profile.Id);

        _bomLines.ListUnlinkedAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { glassLine, hardwareLine, profileLine });

        _glassTypes.GetByIdAsync(glassType.Id, Arg.Any<CancellationToken>()).Returns(glassType);
        _hardware.GetByIdAsync(hardware.Id, Arg.Any<CancellationToken>()).Returns(hardware);
        _profiles.GetByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);

        var glassProductId = Guid.NewGuid();
        var hardwareProductId = Guid.NewGuid();
        var profileProductId = Guid.NewGuid();
        _linker.EnsureLinkedAsync(glassType, CatalogItemKind.Glass, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(glassType.Id, glassProductId, "GE-GLASS-1", false, false));
        _linker.EnsureLinkedAsync(hardware, CatalogItemKind.Hardware, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(hardware.Id, hardwareProductId, "GE-HW-1", false, false));
        _linker.EnsureLinkedAsync(profile, CatalogItemKind.Profile, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(profile.Id, profileProductId, "GE-PRF-1", false, false));

        var result = await BuildSut().Handle(new BomLineBackfillCommand(), default);

        result.TotalScanned.Should().Be(3);
        result.Linked.Should().Be(3);
        result.AlreadyLinked.Should().Be(0);
        result.CouldNotLink.Should().Be(0);
        result.Issues.Should().BeEmpty();

        glassLine.ProductId.Should().Be(glassProductId);
        hardwareLine.ProductId.Should().Be(hardwareProductId);
        profileLine.ProductId.Should().Be(profileProductId);

        _bomLines.Received(3).Update(Arg.Any<GlassProjectBOMLine>());
    }

    [Fact]
    public async Task Handle_skips_already_linked_lines_without_invoking_linker()
    {
        var line = new GlassProjectBOMLine(
            Guid.NewGuid(), GlassBOMLineKind.GlassPiece, "Glass", 1m, "m²", 100m, "TRY",
            refId: Guid.NewGuid(), productId: Guid.NewGuid());

        _bomLines.ListUnlinkedAsync(Arg.Any<CancellationToken>()).Returns(new[] { line });

        var result = await BuildSut().Handle(new BomLineBackfillCommand(), default);

        result.TotalScanned.Should().Be(1);
        result.AlreadyLinked.Should().Be(1);
        result.Linked.Should().Be(0);
        result.CouldNotLink.Should().Be(0);
        await _linker.DidNotReceive().EnsureLinkedAsync(
            Arg.Any<Domain.Common.ICatalogLinkable>(),
            Arg.Any<CatalogItemKind>(),
            Arg.Any<CancellationToken>());
        _bomLines.DidNotReceive().Update(Arg.Any<GlassProjectBOMLine>());
    }

    [Fact]
    public async Task Handle_reports_issue_when_ref_id_is_missing()
    {
        var line = new GlassProjectBOMLine(
            Guid.NewGuid(), GlassBOMLineKind.GlassPiece, "Glass", 1m, "m²", 100m, "TRY",
            refId: null);

        _bomLines.ListUnlinkedAsync(Arg.Any<CancellationToken>()).Returns(new[] { line });

        var result = await BuildSut().Handle(new BomLineBackfillCommand(), default);

        result.TotalScanned.Should().Be(1);
        result.CouldNotLink.Should().Be(1);
        result.Linked.Should().Be(0);
        result.Issues.Should().HaveCount(1);
        result.Issues[0].BomLineId.Should().Be(line.Id);
        result.Issues[0].ReasonKey.Should().Be("bom.refid-missing");
        result.Issues[0].RefId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_invokes_linker_to_create_product_when_catalog_item_has_no_linked_product()
    {
        var glassType = BuildGlassType(linked: false);
        var line = new GlassProjectBOMLine(
            Guid.NewGuid(), GlassBOMLineKind.GlassPiece, "Glass", 1m, "m²", 100m, "TRY",
            refId: glassType.Id);

        _bomLines.ListUnlinkedAsync(Arg.Any<CancellationToken>()).Returns(new[] { line });
        _glassTypes.GetByIdAsync(glassType.Id, Arg.Any<CancellationToken>()).Returns(glassType);

        var createdProductId = Guid.NewGuid();
        _linker.EnsureLinkedAsync(glassType, CatalogItemKind.Glass, Arg.Any<CancellationToken>())
            .Returns(new LinkageResult(glassType.Id, createdProductId, "GE-GLASS-NEW", true, true));

        var result = await BuildSut().Handle(new BomLineBackfillCommand(), default);

        result.TotalScanned.Should().Be(1);
        result.Linked.Should().Be(1);
        result.CouldNotLink.Should().Be(0);
        line.ProductId.Should().Be(createdProductId);
        await _linker.Received(1).EnsureLinkedAsync(glassType, CatalogItemKind.Glass, Arg.Any<CancellationToken>());
        _bomLines.Received(1).Update(line);
    }

    [Fact]
    public async Task Handle_reports_issue_when_catalog_item_cannot_be_resolved()
    {
        var refId = Guid.NewGuid();
        var line = new GlassProjectBOMLine(
            Guid.NewGuid(), GlassBOMLineKind.HardwarePiece, "Hardware", 1m, "Piece", 5m, "TRY",
            refId: refId);

        _bomLines.ListUnlinkedAsync(Arg.Any<CancellationToken>()).Returns(new[] { line });
        _hardware.GetByIdAsync(refId, Arg.Any<CancellationToken>()).Returns((HardwareItem?)null);

        var result = await BuildSut().Handle(new BomLineBackfillCommand(), default);

        result.CouldNotLink.Should().Be(1);
        result.Linked.Should().Be(0);
        result.Issues.Should().HaveCount(1);
        result.Issues[0].ReasonKey.Should().Be("catalog.hardware-not-found");
        result.Issues[0].RefId.Should().Be(refId);
    }

    private static GlassType BuildGlassType(bool linked)
    {
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
        if (linked)
        {
            glass.LinkedProductId = Guid.NewGuid();
        }
        return glass;
    }

    private static HardwareItem BuildHardware(bool linked)
    {
        var hw = new HardwareItem(
            code: "HW-1",
            name: "Hardware One",
            category: HardwareCategoryKind.Other,
            brandId: Guid.NewGuid(),
            unit: "Piece",
            unitPrice: 5m);
        if (linked)
        {
            hw.LinkedProductId = Guid.NewGuid();
        }
        return hw;
    }

    private static ProfileItem BuildProfile(bool linked)
    {
        var profile = new ProfileItem(
            systemId: Guid.NewGuid(),
            role: ProfileRole.Top,
            code: "PRF-1",
            name: "Profile One",
            stockBarLengthMm: 6000,
            weightKgPerMeter: 1.2m,
            pricePerKg: 4.5m);
        if (linked)
        {
            profile.LinkedProductId = Guid.NewGuid();
        }
        return profile;
    }
}
