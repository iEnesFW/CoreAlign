using CoreAlign.Domain.Entities.Purchasing;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IPurchaseRequisitionRepository
{
    Task<PurchaseRequisition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NumberExistsAsync(string number, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PurchaseRequisition> Items, int Total)> SearchAsync(
        PurchaseRequisitionStatus? status,
        Guid? productId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseRequisition requisition, CancellationToken cancellationToken = default);
    void Update(PurchaseRequisition requisition);
    void Remove(PurchaseRequisition requisition);
}
