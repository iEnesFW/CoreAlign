using CoreAlign.Domain.Entities.Sales;

namespace CoreAlign.Domain.Interfaces;

public interface IOrderRevisionRepository
{
    Task<OrderRevision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderRevision>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderRevision?> GetPendingForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddAsync(OrderRevision revision, CancellationToken cancellationToken = default);
    void Update(OrderRevision revision);
}
