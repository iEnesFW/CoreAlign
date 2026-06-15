using CoreAlign.Domain.Entities.Mrp;

namespace CoreAlign.Application.Mrp;

public static class PeggingChainResolver
{
    public static IReadOnlyList<MrpPegging> ResolveUpstream(
        IReadOnlyList<MrpPegging> allPegs,
        Guid componentProductId)
    {
        var byComponent = allPegs
            .GroupBy(p => p.ComponentProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var chain = new List<MrpPegging>();
        var visited = new HashSet<Guid>();
        var current = componentProductId;

        while (visited.Add(current) && byComponent.TryGetValue(current, out var pegs))
        {
            var step = pegs
                .OrderByDescending(p => p.RequirementQuantity)
                .First();
            chain.Add(step);
            if (step.SourceParentProductId is not { } parent)
            {
                break;
            }
            current = parent;
        }

        return chain;
    }
}
