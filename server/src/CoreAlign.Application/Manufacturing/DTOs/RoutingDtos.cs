using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Manufacturing.DTOs;

public record WorkCenterDto(
    Guid Id,
    string Code,
    string Name,
    decimal DailyCapacityMinutes,
    bool IsActive);

public record RoutingStepDto(
    Guid Id,
    int StepNumber,
    Guid WorkCenterId,
    string WorkCenterName,
    string OperationName,
    RoutingOperationType OperationType,
    decimal SetupTimeMinutes,
    decimal RunTimeMinutesPerUnit,
    decimal? RunTimeMinutesPerSqm,
    decimal ScrapPercentage,
    string? Instructions,
    bool IsOptional);

public record ProductionRoutingDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    RoutingStatus Status,
    long ConcurrencyToken,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<RoutingStepDto> Steps);

public record ProductionRoutingSummaryDto(
    Guid Id,
    string Code,
    string Name,
    RoutingStatus Status,
    int StepCount,
    DateTime UpdatedAtUtc);

public record WorkCenterOperatorDto(
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
