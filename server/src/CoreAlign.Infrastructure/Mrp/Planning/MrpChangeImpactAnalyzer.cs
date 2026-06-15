using CoreAlign.Application.Mrp.Planning;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public sealed class MrpChangeImpactAnalyzer : IMrpChangeImpactAnalyzer
{
    public ChangeImpactResult Trace(MrpPlanResult plan, Guid sourceOrderLineId)
    {
        var itemByProduct = plan.Items.ToDictionary(i => i.ProductId);

        var root = plan.Items.FirstOrDefault(i =>
            i.Pegs.Any(p => p.SourceKind == "SalesOrder" && p.SourceOrderLineId == sourceOrderLineId));

        if (root is null)
        {
            return new ChangeImpactResult(sourceOrderLineId, Guid.Empty, Array.Empty<ChangeImpactSupplyOrder>());
        }

        var impactedProducts = CollectDownstreamProducts(plan, root.ProductId);

        var supply = new List<ChangeImpactSupplyOrder>();
        foreach (var productId in impactedProducts)
        {
            if (!itemByProduct.TryGetValue(productId, out var item))
            {
                continue;
            }

            foreach (var order in item.ProductionOrders)
            {
                supply.Add(new ChangeImpactSupplyOrder(
                    item.ProductId,
                    item.Sku,
                    item.LowLevelCode,
                    OrderSinkKind.ProductionOrder,
                    order.Quantity,
                    order.DueDateUtc,
                    order.ReleaseDateUtc,
                    order.PeggingParentProductId));
            }

            foreach (var order in item.PlannedOrders)
            {
                supply.Add(new ChangeImpactSupplyOrder(
                    item.ProductId,
                    item.Sku,
                    item.LowLevelCode,
                    OrderSinkKind.PurchaseRequisition,
                    order.Quantity,
                    order.DueDateUtc,
                    order.ReleaseDateUtc,
                    order.PeggingParentProductId));
            }
        }

        var ordered = supply
            .OrderBy(s => s.LowLevelCode)
            .ThenBy(s => s.DueDateUtc)
            .ToList();

        return new ChangeImpactResult(sourceOrderLineId, root.ProductId, ordered);
    }

    private static HashSet<Guid> CollectDownstreamProducts(MrpPlanResult plan, Guid rootProductId)
    {
        var childrenByParent = plan.Items
            .SelectMany(i => i.Pegs)
            .Where(p => p.SourceParentProductId is not null)
            .GroupBy(p => p.SourceParentProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(p => p.ComponentProductId).Distinct().ToList());

        var impacted = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootProductId);

        while (queue.Count > 0)
        {
            var product = queue.Dequeue();
            if (!impacted.Add(product))
            {
                continue;
            }
            if (childrenByParent.TryGetValue(product, out var children))
            {
                foreach (var child in children)
                {
                    queue.Enqueue(child);
                }
            }
        }

        return impacted;
    }
}
