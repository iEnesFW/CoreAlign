using CoreAlign.Application.Stock.Availability;
using CoreAlign.Application.Stock.Substitute;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using NSubSubstitute = NSubstitute.Substitute;

namespace CoreAlign.Application.Tests.Stock.AvailabilityService;

public class StockAvailabilityServiceTests
{
    private readonly IGlassProjectBOMLineRepository _bomLines = NSubSubstitute.For<IGlassProjectBOMLineRepository>();
    private readonly IStockItemRepository _stockItems = NSubSubstitute.For<IStockItemRepository>();
    private readonly IProductRepository _products = NSubSubstitute.For<IProductRepository>();
    private readonly IProductSubstituteResolver _resolver = NSubSubstitute.For<IProductSubstituteResolver>();
    private readonly IGlassProjectOrderLinkRepository _orderLinks = NSubSubstitute.For<IGlassProjectOrderLinkRepository>();

    private StockAvailabilityService BuildSut() =>
        new(_bomLines, _stockItems, _products, _resolver, _orderLinks);

    [Fact]
    public async Task CheckAsync_returns_empty_when_project_has_no_bom_lines()
    {
        var projectId = Guid.NewGuid();
        _bomLines.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GlassProjectBOMLine>());

        var rows = await BuildSut().CheckAsync(projectId, warehouseId: null);

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAsync_marks_service_line_with_null_product_as_service_and_skips_substitutes()
    {
        var projectId = Guid.NewGuid();
        var serviceLine = new GlassProjectBOMLine(
            projectId,
            GlassBOMLineKind.Labor,
            "Installation labor",
            quantity: 1m,
            unit: "Service",
            unitCost: 100m,
            currency: "TRY",
            productId: null,
            isService: true);

        _bomLines.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new[] { serviceLine });

        var rows = await BuildSut().CheckAsync(projectId, warehouseId: null);

        rows.Should().HaveCount(1);
        rows[0].IsService.Should().BeTrue();
        rows[0].HasShortage.Should().BeFalse();
        rows[0].ProductId.Should().BeNull();
        rows[0].Substitutes.Should().BeEmpty();

        await _resolver.DidNotReceiveWithAnyArgs().ResolveAsync(
            default, default, default, default);
    }

    [Fact]
    public async Task CheckAsync_flags_shortage_only_on_the_line_that_lacks_stock()
    {
        var projectId = Guid.NewGuid();
        var sufficientProductId = Guid.NewGuid();
        var shortageProductId = Guid.NewGuid();

        var sufficientLine = new GlassProjectBOMLine(
            projectId, GlassBOMLineKind.HardwarePiece, "In stock hinge", quantity: 2m,
            unit: "Piece", unitCost: 5m, currency: "TRY", productId: sufficientProductId);
        var shortageLine = new GlassProjectBOMLine(
            projectId, GlassBOMLineKind.GlassPiece, "Short glass", quantity: 10m,
            unit: "m²", unitCost: 100m, currency: "TRY", productId: shortageProductId);

        _bomLines.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new[] { sufficientLine, shortageLine });

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [sufficientProductId] = BuildProduct(sufficientProductId, "HINGE", "Hinge"),
                [shortageProductId] = BuildProduct(shortageProductId, "GLASS", "Glass"),
            });

        _stockItems.SumOnHandAsync(sufficientProductId, Arg.Any<CancellationToken>()).Returns(5m);
        _stockItems.SumReservedAsync(sufficientProductId, Arg.Any<CancellationToken>()).Returns(0m);
        _stockItems.SumOnHandAsync(shortageProductId, Arg.Any<CancellationToken>()).Returns(2m);
        _stockItems.SumReservedAsync(shortageProductId, Arg.Any<CancellationToken>()).Returns(0m);
        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)>
            {
                [sufficientProductId] = (5m, 0m),
                [shortageProductId] = (2m, 0m),
            });

        _resolver.ResolveAsync(shortageProductId, Arg.Any<decimal>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SubstituteSuggestion>());

        var rows = await BuildSut().CheckAsync(projectId, warehouseId: null);

        rows.Should().HaveCount(2);
        var sufficientRow = rows.Single(r => r.ProductId == sufficientProductId);
        var shortageRow = rows.Single(r => r.ProductId == shortageProductId);

        sufficientRow.HasShortage.Should().BeFalse();
        sufficientRow.AvailableQty.Should().Be(5m);
        sufficientRow.ShortageQty.Should().Be(0m);

        shortageRow.HasShortage.Should().BeTrue();
        shortageRow.AvailableQty.Should().Be(2m);
        shortageRow.ShortageQty.Should().Be(8m);
    }

    [Fact]
    public async Task CheckAsync_invokes_substitute_resolver_only_for_lines_with_shortage()
    {
        var projectId = Guid.NewGuid();
        var shortageProductId = Guid.NewGuid();
        var substituteProductId = Guid.NewGuid();

        var shortageLine = new GlassProjectBOMLine(
            projectId, GlassBOMLineKind.GlassPiece, "Short glass", quantity: 10m,
            unit: "m²", unitCost: 100m, currency: "TRY", productId: shortageProductId);

        _bomLines.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new[] { shortageLine });

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [shortageProductId] = BuildProduct(shortageProductId, "GLASS", "Glass"),
            });

        _stockItems.SumOnHandAsync(shortageProductId, Arg.Any<CancellationToken>()).Returns(1m);
        _stockItems.SumReservedAsync(shortageProductId, Arg.Any<CancellationToken>()).Returns(0m);
        _stockItems.SumOnHandAsync(substituteProductId, Arg.Any<CancellationToken>()).Returns(20m);
        _stockItems.SumReservedAsync(substituteProductId, Arg.Any<CancellationToken>()).Returns(0m);
        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(shortageProductId) && !ids.Contains(substituteProductId)),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)>
            {
                [shortageProductId] = (1m, 0m),
            });
        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(substituteProductId)),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)>
            {
                [substituteProductId] = (20m, 0m),
            });

        var suggestion = new SubstituteSuggestion(
            ProductId: substituteProductId,
            ProductSku: "GLASS-ALT",
            ProductName: "Glass Alternative",
            ConversionRate: 1m,
            Priority: 0,
            Depth: 1,
            Notes: null);
        _resolver.ResolveAsync(shortageProductId, Arg.Any<decimal>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { suggestion });

        var rows = await BuildSut().CheckAsync(projectId, warehouseId: null);

        await _resolver.Received(1).ResolveAsync(
            shortageProductId,
            Arg.Any<decimal>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());

        rows.Should().HaveCount(1);
        rows[0].HasShortage.Should().BeTrue();
        rows[0].Substitutes.Should().ContainSingle(s => s.ProductId == substituteProductId);
        rows[0].Substitutes[0].AvailableQty.Should().Be(20m);
    }

    [Fact]
    public async Task CheckAsync_scopes_availability_to_warehouse_when_warehouse_id_is_supplied()
    {
        var projectId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var targetWarehouseId = Guid.NewGuid();
        var otherWarehouseId = Guid.NewGuid();

        var line = new GlassProjectBOMLine(
            projectId, GlassBOMLineKind.HardwarePiece, "Hardware", quantity: 3m,
            unit: "Piece", unitCost: 5m, currency: "TRY", productId: productId);

        _bomLines.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new[] { line });

        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>
            {
                [productId] = BuildProduct(productId, "HW", "Hardware"),
            });

        var scopedItem = new StockItem(productId, targetWarehouseId);
        scopedItem.ApplyReceipt(quantity: 4m, unitCost: 1m, occurredAtUtc: DateTime.UtcNow);
        var otherItem = new StockItem(productId, otherWarehouseId);
        otherItem.ApplyReceipt(quantity: 100m, unitCost: 1m, occurredAtUtc: DateTime.UtcNow);

        _stockItems.GetByProductAsync(productId, Arg.Any<CancellationToken>())
            .Returns(new[] { scopedItem, otherItem });
        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(productId)),
                targetWarehouseId,
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)>
            {
                [productId] = (4m, 0m),
            });

        var rows = await BuildSut().CheckAsync(projectId, warehouseId: targetWarehouseId);

        await _stockItems.Received().SumOnHandAndReservedByProductsAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(productId)),
            targetWarehouseId,
            Arg.Any<CancellationToken>());
        await _stockItems.DidNotReceiveWithAnyArgs().SumOnHandAsync(default, default);

        rows.Should().HaveCount(1);
        rows[0].WarehouseId.Should().Be(targetWarehouseId);
        rows[0].AvailableQty.Should().Be(4m);
        rows[0].HasShortage.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_subtracts_pending_glass_demand_from_other_projects_when_opted_in()
    {
        var projectId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var line = new GlassProjectBOMLine(
            projectId, GlassBOMLineKind.GlassPiece, "Glass", quantity: 8m,
            unit: "m²", unitCost: 100m, currency: "TRY", productId: productId);
        _bomLines.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = BuildProduct(productId, "GLASS", "Glass") });
        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Any<IEnumerable<Guid>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)> { [productId] = (10m, 0m) });
        // 6 units already committed by OTHER projects' pending (Draft/Submitted/Approved) glass orders.
        _orderLinks.SumPendingOrderDemandByProductsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), projectId,
                Arg.Any<IReadOnlyCollection<OrderStatus>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal> { [productId] = 6m });

        var rows = await BuildSut().CheckAsync(projectId, warehouseId: null, accountForPendingDemand: true);

        rows.Should().HaveCount(1);
        rows[0].AvailableQty.Should().Be(4m); // 10 on-hand − 6 pending = 4
        rows[0].HasShortage.Should().BeTrue(); // needs 8, only 4 available after pending demand
        rows[0].ShortageQty.Should().Be(4m);
        await _orderLinks.Received(1).SumPendingOrderDemandByProductsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), projectId,
            Arg.Any<IReadOnlyCollection<OrderStatus>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAsync_ignores_pending_glass_demand_and_does_not_query_links_by_default()
    {
        var projectId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var line = new GlassProjectBOMLine(
            projectId, GlassBOMLineKind.GlassPiece, "Glass", quantity: 8m,
            unit: "m²", unitCost: 100m, currency: "TRY", productId: productId);
        _bomLines.ListByProjectAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new[] { line });
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product> { [productId] = BuildProduct(productId, "GLASS", "Glass") });
        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Any<IEnumerable<Guid>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)> { [productId] = (10m, 0m) });

        var rows = await BuildSut().CheckAsync(projectId, warehouseId: null);

        rows.Should().HaveCount(1);
        rows[0].AvailableQty.Should().Be(10m); // pending demand ignored → full on-hand
        rows[0].HasShortage.Should().BeFalse();
        await _orderLinks.DidNotReceiveWithAnyArgs()
            .SumPendingOrderDemandByProductsAsync(default!, default, default!, default);
    }

    private static Product BuildProduct(Guid id, string sku, string name)
    {
        var product = new Product(sku, name, "pcs", 0m, "TRY");
        product.Id = id;
        return product;
    }
}
