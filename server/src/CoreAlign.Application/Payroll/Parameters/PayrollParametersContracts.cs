using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Payroll.Parameters;

public record PayrollTaxBracketDto(
    Guid Id,
    decimal RatePercent,
    int SortOrder,
    decimal? UpperBound);

public record PayrollParametersDto(
    Guid Id,
    Guid TenantId,
    bool IsGlobal,
    int EffectiveYear,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive,
    string? Description,
    decimal SgkEmployeeRate,
    decimal SgkEmployerRate,
    decimal SgkEmployer5PointIncentiveRate,
    decimal UnemploymentEmployeeRate,
    decimal UnemploymentEmployerRate,
    decimal SgkFloorMonthly,
    decimal SgkCeilingMultiplier,
    decimal SgkCeilingMonthly,
    decimal StampTaxRate,
    decimal GrossMinimumWage,
    bool MinWageExemptionEnabled,
    decimal Disability1Amount,
    decimal Disability2Amount,
    decimal Disability3Amount,
    IReadOnlyList<PayrollTaxBracketDto> TaxBrackets,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreatePayrollTaxBracketInput(decimal RatePercent, int SortOrder, decimal? UpperBound = null);

public record CreatePayrollParametersCommand(
    int EffectiveYear,
    DateOnly EffectiveFrom,
    decimal SgkEmployeeRate,
    decimal SgkEmployerRate,
    decimal SgkEmployer5PointIncentiveRate,
    decimal UnemploymentEmployeeRate,
    decimal UnemploymentEmployerRate,
    decimal SgkFloorMonthly,
    decimal SgkCeilingMultiplier,
    decimal SgkCeilingMonthly,
    decimal StampTaxRate,
    decimal GrossMinimumWage,
    decimal Disability1Amount,
    decimal Disability2Amount,
    decimal Disability3Amount,
    IReadOnlyList<CreatePayrollTaxBracketInput> TaxBrackets,
    bool MinWageExemptionEnabled = true,
    DateOnly? EffectiveTo = null,
    string? Description = null) : IRequest<PayrollParametersDto>, ITransactionalRequest;

public record UpdatePayrollParametersCommand(
    Guid Id,
    decimal SgkEmployeeRate,
    decimal SgkEmployerRate,
    decimal SgkEmployer5PointIncentiveRate,
    decimal UnemploymentEmployeeRate,
    decimal UnemploymentEmployerRate,
    decimal SgkFloorMonthly,
    decimal SgkCeilingMultiplier,
    decimal SgkCeilingMonthly,
    decimal StampTaxRate,
    decimal GrossMinimumWage,
    decimal Disability1Amount,
    decimal Disability2Amount,
    decimal Disability3Amount,
    bool MinWageExemptionEnabled,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    string? Description = null) : IRequest<PayrollParametersDto>, ITransactionalRequest;

public record GetPayrollParametersListQuery(int? Year = null) : IRequest<IReadOnlyList<PayrollParametersDto>>;

public record GetPayrollParametersByIdQuery(Guid Id) : IRequest<PayrollParametersDto?>;
