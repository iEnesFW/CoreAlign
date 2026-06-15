using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IProductSubstituteRepository
{
    Task<IReadOnlyList<ProductSubstitute>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSubstitute>> ListByProductsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
    Task<ProductSubstitute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ProductSubstitute substitute, CancellationToken cancellationToken = default);
    void Update(ProductSubstitute substitute);
    void Remove(ProductSubstitute substitute);
}
