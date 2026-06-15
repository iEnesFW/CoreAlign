using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly CoreAlignDbContext _context;

    public ProductVariantRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Set<ProductVariant>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductVariant>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductVariant>()
            .AsNoTracking()
            .Where(v => v.ParentProductId == productId)
            .OrderBy(v => v.Sku)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SkuExistsAsync(Guid parentProductId, string sku, Guid? excludeVariantId, CancellationToken cancellationToken = default)
    {
        var normalized = sku.Trim();
        var query = _context.Set<ProductVariant>()
            .Where(v => v.ParentProductId == parentProductId && v.Sku == normalized);
        if (excludeVariantId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVariantId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public Task<int> CountByProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => _context.Set<ProductVariant>().CountAsync(v => v.ParentProductId == productId, cancellationToken);

    public Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default)
        => _context.Set<ProductVariant>().AddAsync(variant, cancellationToken).AsTask();

    public void Update(ProductVariant variant)
        => _context.Set<ProductVariant>().Update(variant);

    public void Remove(ProductVariant variant)
        => _context.Set<ProductVariant>().Remove(variant);
}
