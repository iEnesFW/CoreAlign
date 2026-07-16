using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Manufacturing.Mapping;

public static class ProductionJobMapper
{
    public static ProductionJobDetailDto ToDetailDto(
        ProductionJob job,
        string productName,
        IReadOnlyDictionary<Guid, string> workCenterNames)
    {
        var steps = job.Steps
            .OrderBy(s => s.StepNumber)
            .Select(s => ToStepDto(s, workCenterNames))
            .ToList();

        return new ProductionJobDetailDto(
            job.Id,
            job.JobNumber,
            job.ProductId,
            productName,
            job.Status,
            job.PlannedQuantity,
            job.CompletedQuantity,
            job.ScrappedQuantity,
            job.UnitOfMeasure,
            job.WarehouseId,
            job.SourceRoutingId,
            job.RoutingCodeSnapshot,
            job.RoutingNameSnapshot,
            job.RoutingSnapshotVersion,
            job.CurrentStepNumber,
            job.PlannedStartDateUtc,
            job.DueDateUtc,
            job.ReleasedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.CancelledAtUtc,
            job.CancellationReason,
            job.Notes,
            job.ConcurrencyToken,
            job.CreatedAtUtc,
            job.UpdatedAtUtc,
            steps);
    }

    public static ProductionJobStepDto ToStepDto(
        ProductionJobStep step,
        IReadOnlyDictionary<Guid, string> workCenterNames) =>
        new(
            step.Id,
            step.StepNumber,
            step.WorkCenterId,
            step.WorkCenterId.HasValue && workCenterNames.TryGetValue(step.WorkCenterId.Value, out var name) ? name : string.Empty,
            step.OperationName,
            step.OperationType,
            step.Status,
            step.IsOptional,
            step.InputQuantity,
            step.GoodQuantity,
            step.ScrappedQuantity,
            step.SetupTimeMinutes,
            step.RunTimeMinutesPerUnit,
            step.RunTimeMinutesPerSqm,
            step.ScrapPercentage,
            step.ActualSetupMinutes,
            step.ActualRunMinutes,
            step.AssignedOperatorId,
            step.StartedAtUtc,
            step.FinishedAtUtc,
            step.ReworkCount,
            step.Instructions);

    public static ProductionJobListDto ToListDto(ProductionJobListRow row) =>
        new(
            row.Id,
            row.JobNumber,
            row.ProductId,
            row.ProductName,
            row.Status,
            row.PlannedQuantity,
            row.CompletedQuantity,
            row.ScrappedQuantity,
            row.UnitOfMeasure,
            row.CurrentStepNumber,
            row.StepCount,
            row.DueDateUtc,
            row.CreatedAtUtc);
}
