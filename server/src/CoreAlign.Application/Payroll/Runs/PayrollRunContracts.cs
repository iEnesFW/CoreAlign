using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Payroll.Runs;

public record PayrollRunListItemDto(
    Guid Id,
    string RunNumber,
    int PeriodYear,
    int PeriodMonth,
    PayrollRunType RunType,
    PayrollRunStatus Status,
    string Currency,
    decimal TotalGross,
    decimal TotalNet,
    decimal TotalEmployerCost,
    int PayslipCount,
    DateTime? CalculatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? PostedAtUtc,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc);

public record PayrollRunDetailDto(
    Guid Id,
    string RunNumber,
    int PeriodYear,
    int PeriodMonth,
    PayrollRunType RunType,
    PayrollRunStatus Status,
    string Currency,
    Guid ParametersId,
    decimal TotalGross,
    decimal TotalSgkEmployee,
    decimal TotalSgkEmployer,
    decimal TotalUnemploymentEmployee,
    decimal TotalUnemploymentEmployer,
    decimal TotalIncomeTax,
    decimal TotalStampTax,
    decimal TotalDeductions,
    decimal TotalNet,
    decimal TotalEmployerCost,
    int PayslipCount,
    DateTime? CalculatedAtUtc,
    Guid? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    DateTime? PostedAtUtc,
    DateTime? PaidAtUtc,
    DateTime CreatedAtUtc);

public record PayslipEarningLineDto(
    Guid Id,
    SalaryComponentType ComponentType,
    decimal Amount,
    bool TaxExempt,
    bool SgkExempt);

public record PayslipDeductionLineDto(
    Guid Id,
    DeductionType DeductionType,
    decimal Amount,
    bool IsRecurring);

public record PayslipDto(
    Guid Id,
    string PayslipNumber,
    Guid RunId,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeFullName,
    string? NationalIdMasked,
    int PeriodYear,
    int PeriodMonth,
    int DaysWorked,
    Guid ParametersId,
    decimal GrossEarnings,
    decimal SgkBase,
    decimal IncomeTaxBaseThisPeriod,
    decimal CumulativeIncomeTaxBaseBefore,
    decimal CumulativeIncomeTaxBaseAfter,
    decimal CumulativeMinWageBaseBefore,
    decimal CumulativeMinWageBaseAfter,
    decimal SgkEmployee,
    decimal UnemploymentEmployee,
    decimal IncomeTaxGross,
    decimal MinWageIncomeTaxExemptionApplied,
    decimal MinWageStampTaxExemptionApplied,
    decimal DisabilityExemptionApplied,
    decimal IncomeTaxNet,
    decimal StampTax,
    decimal OtherDeductionsTotal,
    decimal NetPay,
    decimal SgkEmployer,
    decimal UnemploymentEmployer,
    decimal EmployerCost,
    IReadOnlyList<PayslipEarningLineDto> EarningLines,
    IReadOnlyList<PayslipDeductionLineDto> DeductionLines);

public record CreatePayrollRunCommand(
    int PeriodYear,
    int PeriodMonth,
    PayrollRunType RunType = PayrollRunType.Regular,
    string Currency = "TRY",
    string? Description = null) : IRequest<PayrollRunDetailDto>, ITransactionalRequest;

public record CalculatePayrollRunCommand(Guid Id) : IRequest<PayrollRunDetailDto>, ITransactionalRequest;

public record ApprovePayrollRunCommand(Guid Id) : IRequest<PayrollRunDetailDto>, ITransactionalRequest;

public record ReopenPayrollRunCommand(Guid Id) : IRequest<PayrollRunDetailDto>, ITransactionalRequest;

public record PostPayrollRunCommand(Guid Id) : IRequest<PayrollRunDetailDto>, ITransactionalRequest;

public record PayPayrollRunCommand(Guid Id) : IRequest<PayrollRunDetailDto>, ITransactionalRequest;

public record GetPayrollRunByIdQuery(Guid Id) : IRequest<PayrollRunDetailDto?>;

public record GetPayrollRunsQuery(
    int? PeriodYear = null,
    PayrollRunStatus? Status = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<PayrollRunListItemDto>>;

public record GetPayslipsByRunQuery(Guid RunId) : IRequest<IReadOnlyList<PayslipDto>>;

public record GetPayslipByIdQuery(Guid Id) : IRequest<PayslipDto?>;
