using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public record GlassProjectListItem(
    Guid Id,
    string Code,
    string ProjectName,
    Guid CustomerId,
    string? CustomerName,
    GlassProjectStatus Status,
    decimal GrandTotal,
    string Currency,
    int TotalPanels,
    decimal TotalAreaM2,
    DateTime UpdatedAtUtc);

public interface IGlassProjectRepository
{
    Task<GlassProject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GlassProject?> GetByIdWithRunsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GlassProject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<GlassProjectListItem> Items, int Total)> SearchAsync(
        string? search,
        GlassProjectStatus? status,
        Guid? customerId,
        Guid? assignedDesignerUserId,
        Guid? assignedSalespersonUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(GlassProject project, CancellationToken cancellationToken = default);
    void Update(GlassProject project);
    void Remove(GlassProject project);
}

public record GlassProjectTemplateListItem(
    Guid Id,
    string Name,
    int WallCount,
    int SlabCount,
    int RunCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public interface IGlassProjectTemplateRepository
{
    Task<GlassProjectTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectTemplateListItem>> ListByUserAsync(Guid createdByUserId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectTemplate template, CancellationToken cancellationToken = default);
    void Remove(GlassProjectTemplate template);
}

public interface IGlassProjectRunRepository
{
    Task<GlassProjectRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GlassProjectRun?> GetByIdWithPanelsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectRun>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectRun run, CancellationToken cancellationToken = default);
    void Update(GlassProjectRun run);
    void Remove(GlassProjectRun run);
}

public interface IRunConnectionRepository
{
    Task<RunConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RunConnection>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(RunConnection connection, CancellationToken cancellationToken = default);
    void Update(RunConnection connection);
    void Remove(RunConnection connection);
}

public interface IGlassProjectPanelRepository
{
    Task<GlassProjectPanel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectPanel>> ListByRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectPanel panel, CancellationToken cancellationToken = default);
    void Update(GlassProjectPanel panel);
    void Remove(GlassProjectPanel panel);
    Task ReplaceHardwareAsync(Guid panelId, IReadOnlyList<(Guid HardwareItemId, decimal Quantity)> items, CancellationToken cancellationToken = default);
}

public interface IGlassProjectSceneRepository
{
    Task<GlassProjectScene?> GetByVersionAsync(Guid projectId, int version, CancellationToken cancellationToken = default);
    Task<GlassProjectScene?> GetLatestAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectScene>> ListVersionsAsync(Guid projectId, int limit, CancellationToken cancellationToken = default);
    Task<int> GetMaxVersionAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectScene scene, CancellationToken cancellationToken = default);
    void Update(GlassProjectScene scene);
}

public interface IGlassProjectChangeLogRepository
{
    Task<IReadOnlyList<GlassProjectChangeLog>> ListByProjectAsync(Guid projectId, int limit, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectChangeLog entry, CancellationToken cancellationToken = default);
}

public interface IGlassProjectAttachmentRepository
{
    Task<GlassProjectAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectAttachment>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectAttachment attachment, CancellationToken cancellationToken = default);
    void Remove(GlassProjectAttachment attachment);
}

public interface IGlassProjectBOMLineRepository
{
    Task<IReadOnlyList<GlassProjectBOMLine>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectBOMLine>> ListByProjectForUpdateAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectBOMLine>> ListUnlinkedAsync(CancellationToken cancellationToken = default);
    Task ReplaceAllForProjectAsync(Guid projectId, IEnumerable<GlassProjectBOMLine> lines, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectBOMLine line, CancellationToken cancellationToken = default);
    void Update(GlassProjectBOMLine line);
    void Remove(GlassProjectBOMLine line);
}

public interface IGlassProjectCuttingPlanRepository
{
    Task<GlassProjectCuttingPlan?> GetLatestAsync(Guid projectId, GlassCuttingPlanType planType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectCuttingPlan>> ListRecentAsync(Guid projectId, GlassCuttingPlanType planType, int limit, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectCuttingPlan plan, CancellationToken cancellationToken = default);
}

public interface IGlassProjectQuoteSnapshotRepository
{
    Task<GlassProjectQuoteSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectQuoteSnapshot>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectQuoteSnapshot snapshot, CancellationToken cancellationToken = default);
}

public interface IGlassProjectShareTokenRepository
{
    Task<GlassProjectShareToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassProjectShareToken>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectShareToken token, CancellationToken cancellationToken = default);
    void Update(GlassProjectShareToken token);
}

public interface IFieldSurveyRepository
{
    Task<FieldSurvey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FieldSurvey>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(FieldSurvey survey, CancellationToken cancellationToken = default);
    void Update(FieldSurvey survey);
    void Remove(FieldSurvey survey);
}

public interface IGlassWorkOrderRepository
{
    Task<GlassWorkOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassWorkOrder>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>Batched fetch (tracked, full entity) for use when callers must enumerate releasable work orders for revision cascades.</summary>
    Task<IReadOnlyList<GlassWorkOrder>> ListReleasableByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassWorkOrder>> ListInRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<decimal> GetWorkloadM2ForDayAsync(DateTime dayUtc, CancellationToken cancellationToken = default);
    Task AddAsync(GlassWorkOrder workOrder, CancellationToken cancellationToken = default);
    void Update(GlassWorkOrder workOrder);
}

public interface IGlassProjectOrderLinkRepository
{
    Task<GlassProjectOrderLink?> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task AddAsync(GlassProjectOrderLink link, CancellationToken cancellationToken = default);

    // Per-product quantity already committed by OTHER glass projects' linked orders that are still in
    // a pre-stock-effect status (nothing reserved or decremented yet) — used to keep the convert-time
    // availability check accurate across concurrent projects without a hard reservation.
    Task<IReadOnlyDictionary<Guid, decimal>> SumPendingOrderDemandByProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        Guid excludeProjectId,
        IReadOnlyCollection<OrderStatus> pendingStatuses,
        CancellationToken cancellationToken = default);
}

public interface IGlassNotificationLogRepository
{
    Task<GlassNotificationLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassNotificationLog>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlassNotificationLog>> ListFailedForRetryAsync(int maxRetries, int batchSize, CancellationToken cancellationToken = default);
    Task AddAsync(GlassNotificationLog log, CancellationToken cancellationToken = default);
    void Update(GlassNotificationLog log);
}
