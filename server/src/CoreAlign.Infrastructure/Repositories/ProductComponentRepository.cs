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
        var frontier = new Queue<Guid>();
        frontier.Enqueue(componentProductId);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (!visited.Add(current)) continue;
            if (current == parentProductId) return true;

            var children = await _context.ProductComponents
                .AsNoTracking()
                .Where(c => c.ParentProductId == current)
                .Select(c => c.ComponentProductId)
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                if (!visited.Contains(child)) frontier.Enqueue(child);
            }
        }

        return false;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>>> GetTreeForProductsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var roots = productIds.Distinct().ToHashSet();
        var resolved = new Dictionary<Guid, IReadOnlyList<(Guid, decimal)>>();
        var frontier = new Queue<Guid>(roots);

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (resolved.ContainsKey(current)) continue;

            var rows = await _context.ProductComponents
                .AsNoTracking()
                .Where(c => c.ParentProductId == current)
                .Select(c => new { c.ComponentProductId, c.Quantity })
                .ToListAsync(cancellationToken);

            var entries = rows.Select(r => (r.ComponentProductId, r.Quantity)).ToList();
            resolved[current] = entries;

            foreach (var (childId, _) in entries)
            {
                if (!resolved.ContainsKey(childId)) frontier.Enqueue(childId);
            }
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
