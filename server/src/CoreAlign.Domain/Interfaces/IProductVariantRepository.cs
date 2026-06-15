using CoreAlign.Domain.Entities.Catalog;

namespace CoreAlign.Domain.Interfaces;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariant>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(Guid parentProductId, string sku, Guid? excludeVariantId, CancellationToken cancellationToken = default);
    Task<int> CountByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    void Update(ProductVariant variant);
    void Remove(ProductVariant variant);
}
