using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ProductSubstituteRepository : IProductSubstituteRepository
{
    private readonly CoreAlignDbContext _context;

    public ProductSubstituteRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductSubstitute>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductSubstitutes
            .AsNoTracking()
            .Where(s => s.ProductId == productId || (s.IsBidirectional && s.SubstituteProductId == productId))
            .OrderBy(s => s.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSubstitute>> ListByProductsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToArray();
        if (ids.Length == 0) return Array.Empty<ProductSubstitute>();

        return await _context.ProductSubstitutes
            .AsNoTracking()
            .Where(s => ids.Contains(s.ProductId) || (s.IsBidirectional && ids.Contains(s.SubstituteProductId)))
            .OrderBy(s => s.Priority)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductSubstitute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.ProductSubstitutes.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task AddAsync(ProductSubstitute substitute, CancellationToken cancellationToken = default)
    {
        await _context.ProductSubstitutes.AddAsync(substitute, cancellationToken);
    }

    public void Update(ProductSubstitute substitute) => _context.ProductSubstitutes.Update(substitute);

    public void Remove(ProductSubstitute substitute) => _context.ProductSubstitutes.Remove(substitute);
}
