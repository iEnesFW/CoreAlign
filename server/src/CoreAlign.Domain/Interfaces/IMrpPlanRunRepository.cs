using CoreAlign.Domain.Entities.Mrp;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IMrpPlanRunRepository
{
    Task AddAsync(MrpPlanRun planRun, CancellationToken cancellationToken = default);
    void Update(MrpPlanRun planRun);

    Task<MrpPlanRun?> GetByIdAsync(Guid id, bool includeChildren, CancellationToken cancellationToken = default);
    Task<MrpPlanRun?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MrpPlanRun> Items, int Total)> SearchPlanRunsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MrpActionMessage> Items, int Total)> SearchActionMessagesAsync(
        Guid? planRunId,
        MrpActionType? actionType,
        MrpActionSeverity? severity,
        Guid? supplierId,
        bool includeDismissed,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<MrpActionMessage?> GetActionMessageByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MrpPlannedOrder?> GetPlannedOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MrpPlannedOrder>> GetPlannedOrdersAsync(
        Guid planRunId,
        IReadOnlyList<Guid> plannedOrderIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MrpPegging>> GetPeggingAsync(
        Guid planRunId,
        Guid componentProductId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MrpPegging>> GetAllPeggingAsync(
        Guid planRunId,
        CancellationToken cancellationToken = default);
}
