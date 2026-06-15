using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IPlannedProductionOrderRepository
{
    Task AddRangeAsync(IReadOnlyList<PlannedProductionOrder> orders, CancellationToken cancellationToken = default);

    Task<PlannedProductionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PlannedProductionOrder> Items, int Total)> SearchAsync(
        Guid? planRunId,
        Guid? productId,
        PlannedProductionOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
