using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public record RoutingSummaryRow(
    Guid Id,
    string Code,
    string Name,
    RoutingStatus Status,
    int StepCount,
    DateTime UpdatedAtUtc);

public record WorkCenterOperatorRow(
    Guid Id,
    Guid WorkCenterId,
    string WorkCenterCode,
    string WorkCenterName,
    Guid EmployeeId,
    string EmployeeName,
    bool EmployeeActive,
    OperatorQualificationLevel QualificationLevel,
    bool IsPrimary,
    bool IsActive,
    DateOnly? CertifiedOn,
    string? Notes);

public interface IProductionRoutingRepository
{
    Task AddAsync(ProductionRouting routing, CancellationToken cancellationToken = default);
    void Remove(ProductionRouting routing);

    // WHY: client-set-PK children on a tracked root are mis-detected as Modified; manage steps explicitly.
    void RemoveSteps(IEnumerable<RoutingStep> steps);
    Task AddStepsAsync(IEnumerable<RoutingStep> steps, CancellationToken cancellationToken = default);

    Task<ProductionRouting?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<ProductionRouting?> GetByIdReadAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsReferencedByProductAsync(Guid tenantId, Guid routingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoutingSummaryRow>> ListSummariesAsync(
        Guid tenantId,
        RoutingStatus? status,
        int take,
        CancellationToken cancellationToken = default);
}

public interface IWorkCenterOperatorRepository
{
    Task AddAsync(WorkCenterOperator op, CancellationToken cancellationToken = default);

    Task<WorkCenterOperator?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<WorkCenterOperatorRow?> GetRowByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<bool> ActiveAssignmentExistsAsync(
        Guid tenantId,
        Guid workCenterId,
        Guid employeeId,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkCenterOperatorRow>> ListAsync(
        Guid tenantId,
        Guid? workCenterId,
        Guid? employeeId,
        int take,
        CancellationToken cancellationToken = default);
}
