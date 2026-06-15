using CoreAlign.Application.Mrp.Planning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Mrp.Planning;

namespace CoreAlign.Application.Tests.Mrp.Planning;

public class ActionMessageGeneratorTests
{
    private readonly ActionMessageGenerator _sut = new();

    private static MrpBucket Bucket(DateTime start, decimal gross = 0m, decimal scheduled = 0m, decimal projected = 0m) =>
        new(start, gross, scheduled, projected, 0m, 0m, 0m);

    private static ActionGenerationContext Context(
        MrpProductSnapshot product,
        IReadOnlyList<MrpBucket> buckets,
        IReadOnlyList<PlannedOrderDraft>? plannedOrders = null,
        IReadOnlyList<ScheduledReceiptBucket>? receipts = null,
        decimal safetyStock = 0m) =>
        new(
            product,
            MrpPlanningTestData.AsOf,
            buckets,
            buckets.Select(b => b.StartUtc).ToList(),
            plannedOrders ?? new List<PlannedOrderDraft>(),
            receipts ?? new List<ScheduledReceiptBucket>(),
            safetyStock,
            0m);

    private static List<MrpBucket> FlatBuckets(int count, decimal projected = 50m)
    {
        var list = new List<MrpBucket>();
        for (var i = 0; i < count; i++)
        {
            list.Add(Bucket(MrpPlanningTestData.AsOf.AddDays(i), projected: projected));
        }
        return list;
    }

    [Fact]
    public void Reschedule_in_when_receipt_arrives_after_first_requirement()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A");
        var buckets = FlatBuckets(6);
        buckets[2] = Bucket(buckets[2].StartUtc, gross: 10m, projected: 50m);
        var poId = Guid.NewGuid();
        var receipts = new[] { new ScheduledReceiptBucket(poId, 10m, MrpPlanningTestData.AsOf.AddDays(5), 5) };

        var messages = _sut.Generate(Context(product, buckets, receipts: receipts));

        messages.Should().Contain(m => m.ActionType == MrpActionType.RescheduleIn && m.RelatedPurchaseOrderId == poId);
    }

    [Fact]
    public void Reschedule_out_when_receipt_arrives_before_first_requirement()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A");
        var buckets = FlatBuckets(6);
        buckets[4] = Bucket(buckets[4].StartUtc, gross: 10m, projected: 50m);
        var poId = Guid.NewGuid();
        var receipts = new[] { new ScheduledReceiptBucket(poId, 10m, MrpPlanningTestData.AsOf.AddDays(1), 1) };

        var messages = _sut.Generate(Context(product, buckets, receipts: receipts));

        messages.Should().Contain(m => m.ActionType == MrpActionType.RescheduleOut && m.RelatedPurchaseOrderId == poId);
    }

    [Fact]
    public void Cancel_supply_when_receipt_never_consumed()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A");
        var buckets = FlatBuckets(6);
        var poId = Guid.NewGuid();
        var receipts = new[] { new ScheduledReceiptBucket(poId, 10m, MrpPlanningTestData.AsOf.AddDays(3), 3) };

        var messages = _sut.Generate(Context(product, buckets, receipts: receipts));

        messages.Should().Contain(m => m.ActionType == MrpActionType.CancelSupply && m.RelatedPurchaseOrderId == poId);
    }

    [Fact]
    public void Below_safety_stock_when_projection_dips_under_safety()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A");
        var buckets = FlatBuckets(4, projected: 50m);
        buckets[2] = Bucket(buckets[2].StartUtc, projected: 5m);

        var messages = _sut.Generate(Context(product, buckets, safetyStock: 20m));

        messages.Should().Contain(m => m.ActionType == MrpActionType.BelowSafetyStock);
    }

    [Fact]
    public void Projected_stockout_when_projection_goes_negative()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A");
        var buckets = FlatBuckets(4, projected: 50m);
        buckets[3] = Bucket(buckets[3].StartUtc, projected: -8m);

        var messages = _sut.Generate(Context(product, buckets, safetyStock: 20m));

        var stockout = messages.Should().ContainSingle(m => m.ActionType == MrpActionType.ProjectedStockout).Subject;
        stockout.Severity.Should().Be(MrpActionSeverity.Critical);
        stockout.Quantity.Should().Be(8m);
        stockout.DaysUntilStockOut.Should().Be(3);
    }

    [Fact]
    public void Release_action_for_future_planned_order()
    {
        var product = MrpPlanningTestData.Product(Guid.NewGuid(), "A");
        var buckets = FlatBuckets(6);
        var planned = new[]
        {
            new PlannedOrderDraft(product.ProductId, 0, 25m,
                MrpPlanningTestData.AsOf.AddDays(5), MrpPlanningTestData.AsOf.AddDays(3),
                null, 0m, LotSizingPolicy.LotForLot)
        };

        var messages = _sut.Generate(Context(product, buckets, plannedOrders: planned));

        messages.Should().Contain(m => m.ActionType == MrpActionType.Release && m.Quantity == 25m);
    }
}
