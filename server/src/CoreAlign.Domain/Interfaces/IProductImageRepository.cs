using CoreAlign.Domain.Entities.Catalog;

namespace CoreAlign.Domain.Interfaces;

public interface IProductImageRepository
{
    Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductImage>> GetByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<int> CountByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, ProductImage>> GetPrimaryByProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default);
    Task AddAsync(ProductImage image, CancellationToken cancellationToken = default);
    void Update(ProductImage image);
    void Remove(ProductImage image);
}
