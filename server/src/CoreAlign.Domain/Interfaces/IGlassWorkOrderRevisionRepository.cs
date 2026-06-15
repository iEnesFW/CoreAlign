using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Domain.Interfaces;

public interface IGlassWorkOrderRevisionRepository
{
    Task<GlassWorkOrderRevision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassWorkOrderRevision>> ListByWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<int> GetMaxRevisionNumberAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<decimal> GetCumulativeSignedDeltaSinceLastApprovalAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<bool> AnyOutstandingBlockingAsync(Guid workOrderId, Guid excludeRevisionId, CancellationToken cancellationToken = default);
    Task<GlassWorkOrderRevision?> GetLatestAsync(Guid workOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, GlassWorkOrderRevision>> GetLatestByWorkOrderIdsAsync(IEnumerable<Guid> workOrderIds, CancellationToken cancellationToken = default);
    Task AddAsync(GlassWorkOrderRevision revision, CancellationToken cancellationToken = default);
    void Update(GlassWorkOrderRevision revision);
}
