using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Stock.Substitute;

public class ProductSubstituteResolver : IProductSubstituteResolver
{
    private readonly IProductSubstituteRepository _substitutes;
    private readonly IProductRepository _products;

    public ProductSubstituteResolver(
        IProductSubstituteRepository substitutes,
        IProductRepository products)
    {
        _substitutes = substitutes;
        _products = products;
    }

    public async Task<IReadOnlyList<SubstituteSuggestion>> ResolveAsync(
        Guid productId,
        decimal requiredQuantity,
        int maxDepth = 3,
        CancellationToken cancellationToken = default)
    {
        if (maxDepth <= 0) return Array.Empty<SubstituteSuggestion>();

        var visited = new HashSet<Guid> { productId };
        var edges = new Dictionary<Guid, (decimal CumulativeRate, int Priority, int Depth, string? Notes)>();
        var frontier = new Dictionary<Guid, decimal> { [productId] = 1m };

        for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
        {
            var levelEdges = await _substitutes.ListByProductsAsync(frontier.Keys, cancellationToken);
            if (levelEdges.Count == 0) break;

            var grouped = new Dictionary<Guid, List<ProductSubstitute>>();
            foreach (var edge in levelEdges)
            {
                if (frontier.ContainsKey(edge.ProductId))
                {
                    AddEdge(grouped, edge.ProductId, edge);
                }
                if (edge.IsBidirectional && frontier.ContainsKey(edge.SubstituteProductId))
                {
                    AddEdge(grouped, edge.SubstituteProductId, edge);
                }
            }

            var nextFrontier = new Dictionary<Guid, decimal>();
            foreach (var (currentId, cumulativeRate) in frontier)
            {
                if (!grouped.TryGetValue(currentId, out var nodeEdges)) continue;
                foreach (var sub in nodeEdges.OrderBy(s => s.Priority))
                {
                    var (nextId, edgeRate) = ResolveEdge(currentId, sub);
                    if (nextId == Guid.Empty) continue;
                    if (!visited.Add(nextId)) continue;

                    var nextRate = cumulativeRate * edgeRate;
                    edges[nextId] = (nextRate, sub.Priority, depth + 1, sub.Notes);
                    nextFrontier[nextId] = nextRate;
                }
            }
            frontier = nextFrontier;
        }

        if (edges.Count == 0) return Array.Empty<SubstituteSuggestion>();

        var productMap = await _products.GetByIdsAsync(edges.Keys, cancellationToken);

        var suggestions = new List<SubstituteSuggestion>(edges.Count);
        foreach (var kv in edges)
        {
            if (!productMap.TryGetValue(kv.Key, out var product)) continue;
            suggestions.Add(new SubstituteSuggestion(
                ProductId: kv.Key,
                ProductSku: product.Sku,
                ProductName: product.Name,
                ConversionRate: kv.Value.CumulativeRate,
                Priority: kv.Value.Priority,
                Depth: kv.Value.Depth,
                Notes: kv.Value.Notes));
        }

        return suggestions
            .OrderBy(s => s.Depth)
            .ThenBy(s => s.Priority)
            .ToList();
    }

    private static void AddEdge(Dictionary<Guid, List<ProductSubstitute>> bucket, Guid key, ProductSubstitute edge)
    {
        if (!bucket.TryGetValue(key, out var list))
        {
            list = new List<ProductSubstitute>();
            bucket[key] = list;
        }
        list.Add(edge);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<SubstituteSuggestion>>> ResolveBatchAsync(
        IEnumerable<Guid> productIds,
        int maxDepth = 3,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, IReadOnlyList<SubstituteSuggestion>>();
        foreach (var pid in productIds.Distinct())
        {
            result[pid] = await ResolveAsync(pid, 1m, maxDepth, cancellationToken);
        }
        return result;
    }

    private static (Guid TargetId, decimal Rate) ResolveEdge(Guid currentId, ProductSubstitute edge)
    {
        if (edge.ProductId == currentId)
        {
            return (edge.SubstituteProductId, edge.ConversionRate);
        }
        if (edge.IsBidirectional && edge.SubstituteProductId == currentId)
        {
            var inverse = edge.ConversionRate == 0m ? 1m : 1m / edge.ConversionRate;
            return (edge.ProductId, inverse);
        }
        return (Guid.Empty, 0m);
    }
}
