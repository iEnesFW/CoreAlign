using CoreAlign.Domain.Entities.Catalog;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ProductImageRepository : IProductImageRepository
{
    private readonly CoreAlignDbContext _context;

    public ProductImageRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Set<ProductImage>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductImage>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductImage>()
            .AsNoTracking()
            .Where(p => p.ProductId == productId)
            .OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.DisplayOrder)
            .ThenBy(p => p.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByProductAsync(Guid productId, CancellationToken cancellationToken = default)
        => _context.Set<ProductImage>().CountAsync(p => p.ProductId == productId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, ProductImage>> GetPrimaryByProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, ProductImage>();
        }
        var rows = await _context.Set<ProductImage>()
            .AsNoTracking()
            .Where(p => ids.Contains(p.ProductId) && p.IsPrimary)
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(r => r.ProductId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task AddAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        await _context.Set<ProductImage>().AddAsync(image, cancellationToken);
    }

    public void Update(ProductImage image)
    {
        _context.Set<ProductImage>().Update(image);
    }

    public void Remove(ProductImage image)
    {
        _context.Set<ProductImage>().Remove(image);
    }
}
