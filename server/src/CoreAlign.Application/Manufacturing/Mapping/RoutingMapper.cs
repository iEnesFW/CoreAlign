using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Manufacturing.Mapping;

public static class RoutingMapper
{
    public static ProductionRoutingDto ToDto(
        ProductionRouting routing,
        IReadOnlyDictionary<Guid, string> workCenterNames)
    {
        var steps = routing.Steps
            .OrderBy(s => s.StepNumber)
            .Select(s => ToStepDto(s, workCenterNames))
            .ToList();

        return new ProductionRoutingDto(
            routing.Id,
            routing.Code,
            routing.Name,
            routing.Description,
            routing.Status,
            routing.ConcurrencyToken,
            routing.CreatedAtUtc,
            routing.UpdatedAtUtc,
            steps);
    }

    public static RoutingStepDto ToStepDto(
        RoutingStep step,
        IReadOnlyDictionary<Guid, string> workCenterNames) =>
        new(
            step.Id,
            step.StepNumber,
            step.WorkCenterId,
            workCenterNames.TryGetValue(step.WorkCenterId, out var name) ? name : string.Empty,
            step.OperationName,
            step.OperationType,
            step.SetupTimeMinutes,
            step.RunTimeMinutesPerUnit,
            step.RunTimeMinutesPerSqm,
            step.ScrapPercentage,
            step.Instructions,
            step.IsOptional);

    public static ProductionRoutingSummaryDto ToSummaryDto(RoutingSummaryRow row) =>
        new(row.Id, row.Code, row.Name, row.Status, row.StepCount, row.UpdatedAtUtc);

    public static WorkCenterDto ToDto(WorkCenter workCenter) =>
        new(workCenter.Id, workCenter.Code, workCenter.Name, workCenter.DailyCapacityMinutes, workCenter.IsActive);

    public static WorkCenterOperatorDto ToDto(WorkCenterOperatorRow row) =>
        new(
            row.Id,
            row.WorkCenterId,
            row.WorkCenterCode,
            row.WorkCenterName,
            row.EmployeeId,
            row.EmployeeName,
            row.EmployeeActive,
            row.QualificationLevel,
            row.IsPrimary,
            row.IsActive,
            row.CertifiedOn,
            row.Notes);
}
