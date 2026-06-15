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

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku)) return Task.FromResult<Product?>(null);
        var trimmed = sku.Trim();
        return _context.Products.FirstOrDefaultAsync(p => p.Sku == trimmed, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Product>> GetBySkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default)
    {
        var skuList = skus?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();
        if (skuList.Length == 0)
        {
            return new Dictionary<string, Product>(StringComparer.Ordinal);
        }
        var rows = await _context.Products
            .Where(p => skuList.Contains(p.Sku))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(p => p.Sku, StringComparer.Ordinal);
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

    public Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        IReadOnlyCollection<Guid>? restrictToIds,
        CancellationToken cancellationToken)
        => SearchInternalAsync(search, isActive, page, pageSize, restrictToIds, cancellationToken);

    public Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => SearchInternalAsync(search, isActive, page, pageSize, null, cancellationToken);

    private async Task<(IReadOnlyList<Product> Items, int Total)> SearchInternalAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        IReadOnlyCollection<Guid>? restrictToIds,
        CancellationToken cancellationToken)
    {
        var query = _context.Products.AsNoTracking();

        if (restrictToIds is not null)
        {
            if (restrictToIds.Count == 0)
            {
                return (Array.Empty<Product>(), 0);
            }
            var idArray = restrictToIds.Distinct().ToArray();
            query = query.Where(p => idArray.Contains(p.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = $"%{search.Trim().ToLower()}%";
            if (_context.Database.IsNpgsql())
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Name, lower) ||
                    EF.Functions.ILike(p.Sku, lower) ||
                    (p.Barcode != null && EF.Functions.ILike(p.Barcode, lower)) ||
                    (p.Description != null && EF.Functions.ILike(p.Description, lower)));
            }
            else
            {
                query = query.Where(p =>
                    EF.Functions.Like(p.Name.ToLower(), lower) ||
                    EF.Functions.Like(p.Sku.ToLower(), lower) ||
                    (p.Barcode != null && EF.Functions.Like(p.Barcode.ToLower(), lower)) ||
                    (p.Description != null && EF.Functions.Like(p.Description.ToLower(), lower)));
            }
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
            .ThenBy(p => p.Id)
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
