using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Events;

public record EmployeeSalaryChangedEvent(
    Guid TenantId,
    Guid EmployeeId,
    string EmployeeNumber,
    decimal PreviousBaseSalaryGross,
    decimal NewBaseSalaryGross,
    DateOnly EffectiveDate,
    DateTime OccurredAtUtc) : IDomainEvent;

public record PayrollRunPostedEvent(
    Guid TenantId,
    Guid PayrollRunId,
    string RunNumber,
    int PeriodYear,
    int PeriodMonth,
    decimal TotalNet,
    decimal TotalEmployerCost,
    DateTime OccurredAtUtc) : IDomainEvent;

public record PayrollRunPaidEvent(
    Guid TenantId,
    Guid PayrollRunId,
    string RunNumber,
    int PeriodYear,
    int PeriodMonth,
    decimal TotalNet,
    DateTime OccurredAtUtc) : IDomainEvent;
