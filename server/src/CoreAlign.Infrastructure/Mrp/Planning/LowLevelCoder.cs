using CoreAlign.Application.Mrp.Planning;

namespace CoreAlign.Infrastructure.Mrp.Planning;

public static class LowLevelCoder
{
    public static IReadOnlyDictionary<Guid, int> Assign(
        IReadOnlyList<Guid> productIds,
        IReadOnlyList<BomEdgeSnapshot> edges)
    {
        var childrenByParent = edges
            .GroupBy(e => e.ParentProductId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ComponentProductId).ToList());

        var levels = productIds.ToDictionary(id => id, _ => 0);

        foreach (var rootId in productIds)
        {
            Propagate(rootId, 0, childrenByParent, levels, new HashSet<Guid>());
        }

        return levels;
    }

    private static void Propagate(
        Guid productId,
        int depth,
        IReadOnlyDictionary<Guid, List<Guid>> childrenByParent,
        Dictionary<Guid, int> levels,
        HashSet<Guid> path)
    {
        if (!path.Add(productId))
        {
            return;
        }

        if (levels.TryGetValue(productId, out var current))
        {
            if (depth > current)
            {
                levels[productId] = depth;
            }
        }
        else
        {
            levels[productId] = depth;
        }

        if (childrenByParent.TryGetValue(productId, out var children))
        {
            foreach (var child in children)
            {
                Propagate(child, depth + 1, childrenByParent, levels, path);
            }
        }

        path.Remove(productId);
    }
}
