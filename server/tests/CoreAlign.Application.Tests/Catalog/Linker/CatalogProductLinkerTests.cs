using CoreAlign.Application.Catalog.Linker;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Catalog.Linker;

public class CatalogProductLinkerTests
{
    private readonly ISkuStrategy _skuStrategy = Substitute.For<ISkuStrategy>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IGlassTypeRepository _glassTypes = Substitute.For<IGlassTypeRepository>();
    private readonly IHardwareItemRepository _hardware = Substitute.For<IHardwareItemRepository>();
    private readonly IProfileItemRepository _profiles = Substitute.For<IProfileItemRepository>();
    private readonly IProfileSystemRepository _profileSystems = Substitute.For<IProfileSystemRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private readonly Guid _tenantId = Guid.NewGuid();

    public CatalogProductLinkerTests()
    {
        _tenantContext.RequireTenantId().Returns(_tenantId);
        _tenantContext.CurrentTenantId.Returns(_tenantId);

        _glassTypes.ListAsync(Arg.Any<bool?>(), Arg.Any<GlassStructure?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GlassType>());
        _hardware.ListAsync(Arg.Any<bool?>(), Arg.Any<HardwareCategoryKind?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HardwareItem>());
        _profileSystems.ListAsync(Arg.Any<bool?>(), Arg.Any<Guid?>(), Arg.Any<GlassSystemType?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProfileSystem>());
    }

    private CatalogProductLinker BuildSut() =>
        new(_skuStrategy, _products, _glassTypes, _hardware, _profiles, _profileSystems, _tenantContext);

    [Fact]
    public async Task EnsureLinkedAsync_is_idempotent_when_link_already_set()
    {
        var item = new FakeCatalogItem { LinkedProductId = Guid.NewGuid() };
        var existing = new Product("EXISTING-SKU", "Existing");
        _products.GetByIdAsync(item.LinkedProductId!.Value, Arg.Any<CancellationToken>()).Returns(existing);
        var sut = BuildSut();

        var result = await sut.EnsureLinkedAsync(item, CatalogItemKind.Glass);

        result.ProductCreated.Should().BeFalse();
        result.LinkUpdated.Should().BeFalse();
        result.ProductId.Should().Be(existing.Id);
        await _products.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        _skuStrategy.DidNotReceive().BuildSku(Arg.Any<SkuContext>());
    }

    [Fact]
    public async Task EnsureLinkedAsync_links_to_existing_product_with_same_sku()
    {
        var item = new FakeCatalogItem { Code = "T6" };
        var existingProduct = new Product("GE-GLASS-T6", "Glass T6");
        _skuStrategy.BuildSku(Arg.Any<SkuContext>()).Returns("GE-GLASS-T6");
        _products.GetBySkuAsync("GE-GLASS-T6", Arg.Any<CancellationToken>()).Returns(existingProduct);
        var sut = BuildSut();

        var result = await sut.EnsureLinkedAsync(item, CatalogItemKind.Glass);

        result.ProductCreated.Should().BeFalse();
        result.LinkUpdated.Should().BeTrue();
        result.ProductId.Should().Be(existingProduct.Id);
        item.LinkedProductId.Should().Be(existingProduct.Id);
        await _products.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureLinkedAsync_creates_new_product_when_none_exists()
    {
        var item = new FakeCatalogItem { Code = "NEW-1", Name = "Brand New", Unit = "Piece", UnitCost = 12.5m };
        _skuStrategy.BuildSku(Arg.Any<SkuContext>()).Returns("GE-HW-NEW-1");
        _products.GetBySkuAsync("GE-HW-NEW-1", Arg.Any<CancellationToken>()).Returns((Product?)null);
        var sut = BuildSut();

        var result = await sut.EnsureLinkedAsync(item, CatalogItemKind.Hardware);

        result.ProductCreated.Should().BeTrue();
        result.LinkUpdated.Should().BeTrue();
        result.Sku.Should().Be("GE-HW-NEW-1");
        item.LinkedProductId.Should().NotBeNull();
        item.LinkedProductId.Should().Be(result.ProductId);
        await _products.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Sku == "GE-HW-NEW-1" && p.Name == "Brand New" && p.Unit == "Piece"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunDryRunAsync_reports_counts_correctly()
    {
        var glassLinked = BuildGlass("G-A", linked: true);
        var glassUnlinked = BuildGlass("G-B", linked: false);
        var hardwareUnlinked = BuildHardware("H-A", linked: false);

        _glassTypes.ListAsync(Arg.Any<bool?>(), Arg.Any<GlassStructure?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { glassLinked, glassUnlinked });
        _hardware.ListAsync(Arg.Any<bool?>(), Arg.Any<HardwareCategoryKind?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { hardwareUnlinked });

        _skuStrategy.BuildSku(Arg.Is<SkuContext>(c => c.Kind == CatalogItemKind.Glass)).Returns("GE-GLASS-G-B");
        _skuStrategy.BuildSku(Arg.Is<SkuContext>(c => c.Kind == CatalogItemKind.Hardware)).Returns("GE-HW-H-A");

        _products.GetBySkusAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Product>());

        var sut = BuildSut();

        var report = await sut.RunDryRunAsync();

        report.TotalCatalogItems.Should().Be(3);
        report.AlreadyLinked.Should().Be(1);
        report.ToBeLinked.Should().Be(2);
        report.SkuConflicts.Should().Be(0);
        report.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task RunDryRunAsync_detects_sku_conflicts_across_kinds()
    {
        var glass = BuildGlass("CLASH", linked: false);
        var hardware = BuildHardware("CLASH", linked: false);

        _glassTypes.ListAsync(Arg.Any<bool?>(), Arg.Any<GlassStructure?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { glass });
        _hardware.ListAsync(Arg.Any<bool?>(), Arg.Any<HardwareCategoryKind?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { hardware });

        _skuStrategy.BuildSku(Arg.Is<SkuContext>(c => c.Kind == CatalogItemKind.Glass)).Returns("GE-CLASH");
        _skuStrategy.BuildSku(Arg.Is<SkuContext>(c => c.Kind == CatalogItemKind.Hardware)).Returns("GE-CLASH");

        var conflictingProduct = new Product("GE-CLASH", "Existing");
        _products.GetBySkusAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Product> { ["GE-CLASH"] = conflictingProduct });

        var sut = BuildSut();

        var report = await sut.RunDryRunAsync();

        report.SkuConflicts.Should().Be(2);
        report.ToBeLinked.Should().Be(0);
        report.Conflicts.Should().HaveCount(2);
        report.Conflicts.Should().OnlyContain(c => c.ProposedSku == "GE-CLASH" && c.ConflictingProductId == conflictingProduct.Id);
        report.Conflicts.Select(c => c.Kind).Should().Contain(new[] { CatalogItemKind.Glass, CatalogItemKind.Hardware });
    }

    private static GlassType BuildGlass(string code, bool linked)
    {
        var glass = new GlassType(
            code: code,
            name: code,
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

    private static HardwareItem BuildHardware(string code, bool linked)
    {
        var hw = new HardwareItem(
            code: code,
            name: code,
            category: HardwareCategoryKind.Other,
            brandId: Guid.NewGuid(),
            unit: "Piece",
            unitPrice: 10m);
        if (linked)
        {
            hw.LinkedProductId = Guid.NewGuid();
        }
        return hw;
    }

    private sealed class FakeCatalogItem : ICatalogLinkable
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Code { get; set; } = "CODE";
        public string Name { get; set; } = "Name";
        public string Unit { get; set; } = "pcs";
        public decimal UnitCost { get; set; } = 1m;
        public Guid? LinkedProductId { get; set; }
    }
}
