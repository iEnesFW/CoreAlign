using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly CoreAlignDbContext _context;

    public ProductRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<bool> SkuExistsAsync(string sku, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Where(p => p.Sku == sku);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return query.AnyAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        var products = await _context.Products
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
        return products.ToDictionary(p => p.Id);
    }

    public async Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().ToLower()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name.ToLower(), pattern) ||
                EF.Functions.Like(p.Sku.ToLower(), pattern) ||
                (p.Barcode != null && EF.Functions.Like(p.Barcode.ToLower(), pattern)) ||
                (p.Description != null && EF.Functions.Like(p.Description.ToLower(), pattern)));
        }

        if (isActive.HasValue)
        {
            query = isActive.Value
                ? query.Where(p => p.Status == ProductStatus.Active || p.Status == ProductStatus.New)
                : query.Where(p => p.Status == ProductStatus.Discontinued || p.Status == ProductStatus.EndOfLife);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public void Remove(Product product)
    {
        _context.Products.Remove(product);
    }
}
