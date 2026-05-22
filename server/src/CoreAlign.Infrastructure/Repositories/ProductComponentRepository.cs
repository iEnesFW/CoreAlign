using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ProductComponentRepository : IProductComponentRepository
{
    private readonly CoreAlignDbContext _context;

    public ProductComponentRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<ProductComponent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.ProductComponents
            .Include(c => c.ParentProduct)
            .Include(c => c.ComponentProduct)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductComponent>> GetByParentAsync(Guid parentProductId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ProductComponents
            .AsNoTracking()
            .Include(c => c.ComponentProduct)
            .Where(c => c.ParentProductId == parentProductId)
            .OrderBy(c => c.ComponentProduct.Name)
            .ToListAsync(cancellationToken);
        return items;
    }

    public Task<bool> ExistsAsync(Guid parentProductId, Guid componentProductId, CancellationToken cancellationToken = default)
        => _context.ProductComponents
            .AsNoTracking()
            .AnyAsync(c => c.ParentProductId == parentProductId && c.ComponentProductId == componentProductId, cancellationToken);

    public async Task<bool> WouldCreateCycleAsync(Guid parentProductId, Guid componentProductId, CancellationToken cancellationToken = default)
    {
        if (parentProductId == componentProductId) return true;

        var visited = new HashSet<Guid>();
        var current = new HashSet<Guid> { componentProductId };

        // Wave-batch traversal: instead of one query per node (depth × breadth),
        // load every child of the current wave in a single round-trip, then move
        // on. Total cost is O(treeDepth) queries.
        while (current.Count > 0)
        {
            if (current.Contains(parentProductId)) return true;

            foreach (var id in current) visited.Add(id);

            var snapshot = current;
            var children = await _context.ProductComponents
                .AsNoTracking()
                .Where(c => snapshot.Contains(c.ParentProductId))
                .Select(c => c.ComponentProductId)
                .ToListAsync(cancellationToken);

            current = children.Where(c => !visited.Contains(c)).ToHashSet();
        }

        return false;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>>> GetTreeForProductsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var resolved = new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>();
        var current = productIds.Distinct().ToHashSet();

        while (current.Count > 0)
        {
            // Single round-trip for the entire wave; replaces the old N+1 loop.
            var snapshot = current;
            var rows = await _context.ProductComponents
                .AsNoTracking()
                .Where(c => snapshot.Contains(c.ParentProductId))
                .Select(c => new { c.ParentProductId, c.ComponentProductId, c.Quantity })
                .ToListAsync(cancellationToken);

            var grouped = rows
                .GroupBy(r => r.ParentProductId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<(Guid, decimal)>)g
                        .Select(r => (r.ComponentProductId, r.Quantity))
                        .ToList());

            var next = new HashSet<Guid>();
            foreach (var parent in current)
            {
                if (grouped.TryGetValue(parent, out var entries))
                {
                    resolved[parent] = entries;
                    foreach (var (childId, _) in entries)
                    {
                        if (!resolved.ContainsKey(childId)) next.Add(childId);
                    }
                }
                else
                {
                    resolved[parent] = Array.Empty<(Guid, decimal)>();
                }
            }

            current = next;
        }

        return resolved;
    }

    public async Task AddAsync(ProductComponent component, CancellationToken cancellationToken = default)
    {
        await _context.ProductComponents.AddAsync(component, cancellationToken);
    }

    public void Update(ProductComponent component) => _context.ProductComponents.Update(component);
    public void Remove(ProductComponent component) => _context.ProductComponents.Remove(component);
}
