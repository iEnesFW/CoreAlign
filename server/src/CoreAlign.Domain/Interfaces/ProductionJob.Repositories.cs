using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public record ProductionJobListRow(
    Guid Id,
    string JobNumber,
    Guid ProductId,
    string ProductName,
    ProductionJobStatus Status,
    decimal PlannedQuantity,
    decimal CompletedQuantity,
    decimal ScrappedQuantity,
    string UnitOfMeasure,
    int? CurrentStepNumber,
    int StepCount,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc);

public interface IProductionJobRepository
{
    Task AddAsync(ProductionJob job, CancellationToken ct = default);

    Task<ProductionJob?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);

    Task<bool> JobNumberExistsAsync(Guid tenantId, string jobNumber, CancellationToken ct = default);

    Task<IReadOnlyList<ProductionJobListRow>> ListAsync(
        Guid tenantId,
        ProductionJobStatus? status,
        Guid? productId,
        int take,
        CancellationToken ct = default);
}
