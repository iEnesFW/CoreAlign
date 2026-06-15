using CoreAlign.Application.B2B;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Mrp;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Mrp;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Mrp;

public class MrpServiceTests
{
    private readonly IStockItemRepository _stockItems = Substitute.For<IStockItemRepository>();
    private readonly IPurchaseRequisitionRepository _requisitions = Substitute.For<IPurchaseRequisitionRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IOutboxRepository _outbox = Substitute.For<IOutboxRepository>();
    private readonly IOutboxSignal _outboxSignal = Substitute.For<IOutboxSignal>();

    private MrpService Build(MrpDbContextFixture fixture)
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        var seq = 0;
        _sequences.ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => $"PR-2026-{++seq:D5}");
        return new MrpService(
            fixture.Db,
            _stockItems,
            _requisitions,
            _sequences,
            fixture.TenantContext,
            _currentUser,
            _outbox,
            _outboxSignal,
            NullLogger<MrpService>.Instance);
    }

    private async Task<Product> SeedProductAsync(MrpDbContextFixture fx, decimal safetyStock = 0m, int leadTimeDays = 0, decimal reorderPoint = 0m, decimal maxStock = 0m)
    {
        var product = new Product("SKU-1", "Widget", "pcs", 10m, "TRY")
        {
            TenantId = fx.TenantId,
        };
        product.Update(
            sku: "SKU-1", barcode: null, mpn: null, name: "Widget",
            shortDescription: null, description: null, slug: null,
            brandId: null, categoryId: null, parentProductId: null,
            variantAttributesJson: null, tagsJson: null,
            unit: "pcs", baseUomId: null, purchaseUomId: null, salesUomId: null,
            listPrice: 10m, price: 10m, minSellingPrice: 0m,
            standardCost: 5m, currency: "TRY", taxRateId: null, isPriceTaxInclusive: false,
            isStockTracked: true, isLotTracked: false, isSerialTracked: false,
            minStock: 0m, maxStock: maxStock, reorderPoint: reorderPoint,
            safetyStock: safetyStock, leadTimeDays: leadTimeDays,
            weightKg: null, widthCm: null, heightCm: null, depthCm: null, volumeM3: null,
            status: ProductStatus.Active, launchDate: null, endOfLifeDate: null);
        fx.Db.Products.Add(product);
        await fx.Db.SaveChangesAsync();
        return product;
    }

    private static OrderLine BuildShippedLine(Guid tenantId, Guid productId, decimal shipped, DateTime atUtc)
    {
        var line = new OrderLine(productId, "SKU-1", "Widget", shipped, 10m) { TenantId = tenantId };
        typeof(OrderLine).GetProperty(nameof(OrderLine.QuantityShipped))!.SetValue(line, shipped);
        typeof(OrderLine).BaseType!.GetProperty(nameof(OrderLine.UpdatedAtUtc))!.SetValue(line, atUtc);
        return line;
    }

    private static OrderLine BuildAllocatedLine(Guid tenantId, Guid productId, decimal allocated)
    {
        var line = new OrderLine(productId, "SKU-1", "Widget", allocated, 10m) { TenantId = tenantId };
        typeof(OrderLine).GetProperty(nameof(OrderLine.QuantityAllocated))!.SetValue(line, allocated);
        typeof(OrderLine).GetProperty(nameof(OrderLine.Status))!.SetValue(line, OrderLineStatus.Allocated);
        return line;
    }

    [Fact]
    public async Task ProjectStockBalanceAsync_does_not_double_subtract_reserved_and_committed()
    {
        await using var fx = await MrpDbContextFixture.CreateAsync();
        var product = await SeedProductAsync(fx, safetyStock: 0m, leadTimeDays: 0, reorderPoint: 10m, maxStock: 100m);

        fx.Db.OrderLines.Add(BuildAllocatedLine(fx.TenantId, product.Id, 20m));
        await fx.Db.SaveChangesAsync();

        _stockItems.SumOnHandAsync(product.Id, Arg.Any<CancellationToken>()).Returns(50m);
        _stockItems.SumReservedAsync(product.Id, Arg.Any<CancellationToken>()).Returns(20m);

        var svc = Build(fx);
        var dto = await svc.ProjectStockBalanceAsync(product.Id, daysAhead: 5);

        dto.Should().NotBeNull();
        dto!.TotalCommitted.Should().Be(20m);
        dto.CurrentReserved.Should().Be(20m);
        dto.Points[0].ProjectedQuantity.Should().Be(30m);
        dto.ShouldReorder.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateDemandForecastAsync_averages_shipped_qty_over_window()
    {
        await using var fx = await MrpDbContextFixture.CreateAsync();
        var product = await SeedProductAsync(fx);

        var now = DateTime.UtcNow;
        fx.Db.OrderLines.Add(BuildShippedLine(fx.TenantId, product.Id, 30m, now.AddDays(-10)));
        fx.Db.OrderLines.Add(BuildShippedLine(fx.TenantId, product.Id, 60m, now.AddDays(-20)));
        await fx.Db.SaveChangesAsync();

        var svc = Build(fx);
        var dto = await svc.CalculateDemandForecastAsync(product.Id, windowDays: 90);

        dto.Should().NotBeNull();
        dto!.TotalDemand.Should().Be(90m);
        dto.AverageDailyDemand.Should().Be(Math.Round(90m / 90m, 4));
        dto.WindowDays.Should().Be(90);
    }

    [Fact]
    public async Task CalculateReorderPointAsync_uses_safetyStock_plus_leadTime_times_avgDaily_times_1_2()
    {
        await using var fx = await MrpDbContextFixture.CreateAsync();
        var product = await SeedProductAsync(fx, safetyStock: 5m, leadTimeDays: 7);

        var now = DateTime.UtcNow;
        fx.Db.OrderLines.Add(BuildShippedLine(fx.TenantId, product.Id, 90m, now.AddDays(-15)));
        await fx.Db.SaveChangesAsync();

        var svc = Build(fx);
        var dto = await svc.CalculateReorderPointAsync(product.Id);

        var expectedAvg = Math.Round(90m / 90m, 4);
        var expected = Math.Round(5m + (7 * expectedAvg * MrpService.DemandSafetyFactor), 4);

        dto.Should().NotBeNull();
        dto!.AverageDailyDemand.Should().Be(expectedAvg);
        dto.ComputedReorderPoint.Should().Be(expected);
        dto.SafetyStock.Should().Be(5m);
        dto.LeadTimeDays.Should().Be(7);
    }

    [Fact]
    public async Task ProjectStockBalanceAsync_projects_current_plus_onOrder_minus_committed()
    {
        await using var fx = await MrpDbContextFixture.CreateAsync();
        var product = await SeedProductAsync(fx, safetyStock: 0m, leadTimeDays: 0, reorderPoint: 10m, maxStock: 100m);

        _stockItems.SumOnHandAsync(product.Id, Arg.Any<CancellationToken>()).Returns(50m);
        _stockItems.SumReservedAsync(product.Id, Arg.Any<CancellationToken>()).Returns(5m);

        var svc = Build(fx);
        var dto = await svc.ProjectStockBalanceAsync(product.Id, daysAhead: 5);

        dto.Should().NotBeNull();
        dto!.CurrentOnHand.Should().Be(50m);
        dto.CurrentReserved.Should().Be(5m);
        dto.TotalOnOrder.Should().Be(0m);
        dto.TotalCommitted.Should().Be(0m);
        dto.ReorderPoint.Should().Be(10m);
        dto.Points.Should().HaveCount(6);
        dto.Points[0].ProjectedQuantity.Should().Be(45m);
        dto.ShouldReorder.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateRequisitionSuggestionsAsync_creates_suggestion_when_projection_below_reorder()
    {
        await using var fx = await MrpDbContextFixture.CreateAsync();
        var product = await SeedProductAsync(fx, safetyStock: 0m, leadTimeDays: 3, reorderPoint: 100m, maxStock: 200m);

        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)>
            {
                [product.Id] = (10m, 0m)
            });

        PurchaseRequisition? captured = null;
        await _requisitions.AddAsync(Arg.Do<PurchaseRequisition>(r => captured = r), Arg.Any<CancellationToken>());

        var svc = Build(fx);
        var result = await svc.GenerateRequisitionSuggestionsAsync(DateTime.UtcNow);

        result.RequisitionsCreated.Should().Be(1);
        result.LinesCreated.Should().BeGreaterThan(0);
        captured.Should().NotBeNull();
        captured!.Lines.Should().NotBeEmpty();
        await _outbox.Received(1).AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
        _outboxSignal.Received(1).MarkPending();
    }

    [Fact]
    public async Task GenerateRequisitionSuggestionsAsync_creates_no_suggestion_when_projection_above_reorder()
    {
        await using var fx = await MrpDbContextFixture.CreateAsync();
        var product = await SeedProductAsync(fx, safetyStock: 0m, leadTimeDays: 0, reorderPoint: 10m, maxStock: 100m);

        _stockItems.SumOnHandAndReservedByProductsAsync(
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, (decimal OnHand, decimal Reserved)>
            {
                [product.Id] = (500m, 0m)
            });

        var svc = Build(fx);
        var result = await svc.GenerateRequisitionSuggestionsAsync(DateTime.UtcNow);

        result.RequisitionsCreated.Should().Be(0);
        result.LinesCreated.Should().Be(0);
        await _requisitions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _outbox.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
