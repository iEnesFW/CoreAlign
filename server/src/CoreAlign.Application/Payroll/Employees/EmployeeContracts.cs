using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Payroll.Employees;

public record SalaryComponentDto(
    Guid Id,
    SalaryComponentType ComponentType,
    decimal Amount,
    bool IsRecurring,
    bool TaxExempt,
    bool SgkExempt,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive);

public record EmployeeDeductionDto(
    Guid Id,
    DeductionType DeductionType,
    decimal? Amount,
    decimal? Percent,
    decimal RemainingBalance,
    int Priority,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    bool IsActive);

public record EmployeeListItemDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string FullName,
    string? NationalIdMasked,
    EmploymentStatus Status,
    EmploymentType EmploymentType,
    string? Department,
    string? Title,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    decimal BaseSalaryGross,
    string SalaryCurrency,
    string? IbanMasked);

public record EmployeeDetailDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string FullName,
    string? NationalIdMasked,
    string? SgkRegistrationNo,
    string? Email,
    string? Phone,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    EmploymentStatus Status,
    string? Department,
    string? Title,
    EmploymentType EmploymentType,
    SalaryBasis SalaryBasis,
    decimal BaseSalaryGross,
    string SalaryCurrency,
    string? IbanMasked,
    string? BankName,
    bool IsSgkIncentiveEligible,
    DisabilityDegree DisabilityDegree,
    bool IsRetiredWorking,
    bool SgkExempt,
    int DependentCount,
    bool SpouseEmployed,
    string? TerminationReason,
    IReadOnlyList<SalaryComponentDto> SalaryComponents,
    IReadOnlyList<EmployeeDeductionDto> Deductions,
    DateTime CreatedAtUtc);

public record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string NationalId,
    DateOnly HireDate,
    decimal BaseSalaryGross,
    EmploymentType EmploymentType = EmploymentType.FullTime,
    SalaryBasis SalaryBasis = SalaryBasis.Gross,
    string SalaryCurrency = "TRY",
    string? SgkRegistrationNo = null,
    string? Email = null,
    string? Phone = null,
    string? Department = null,
    string? Title = null,
    string? Iban = null,
    string? BankName = null,
    bool IsSgkIncentiveEligible = false,
    DisabilityDegree DisabilityDegree = DisabilityDegree.None,
    bool IsRetiredWorking = false,
    bool SgkExempt = false,
    int DependentCount = 0,
    bool SpouseEmployed = false,
    Guid? UserId = null) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? Department,
    string? Title,
    string? Iban,
    string? BankName,
    int DependentCount,
    bool SpouseEmployed) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record ChangeBaseSalaryCommand(
    Guid Id,
    decimal BaseSalaryGross,
    DateOnly EffectiveDate) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record PlaceEmployeeOnLeaveCommand(Guid Id) : IRequest<EmployeeDetailDto>, ITransactionalRequest;
public record ReturnEmployeeFromLeaveCommand(Guid Id) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record TerminateEmployeeCommand(
    Guid Id,
    DateOnly TerminationDate,
    string? Reason = null) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record AddSalaryComponentCommand(
    Guid EmployeeId,
    SalaryComponentType ComponentType,
    decimal Amount,
    DateOnly EffectiveFrom,
    bool IsRecurring = true,
    bool TaxExempt = false,
    bool SgkExempt = false,
    DateOnly? EffectiveTo = null) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record UpdateSalaryComponentCommand(
    Guid EmployeeId,
    Guid ComponentId,
    decimal Amount,
    DateOnly EffectiveFrom,
    bool IsRecurring,
    bool TaxExempt,
    bool SgkExempt,
    DateOnly? EffectiveTo = null) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record DeactivateSalaryComponentCommand(
    Guid EmployeeId,
    Guid ComponentId,
    DateOnly EffectiveTo) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record AddDeductionCommand(
    Guid EmployeeId,
    DeductionType DeductionType,
    DateOnly EffectiveFrom,
    decimal? Amount = null,
    decimal? Percent = null,
    decimal RemainingBalance = 0m,
    int Priority = 0,
    DateOnly? EffectiveTo = null) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record UpdateDeductionCommand(
    Guid EmployeeId,
    Guid DeductionId,
    DateOnly EffectiveFrom,
    decimal? Amount = null,
    decimal? Percent = null,
    decimal RemainingBalance = 0m,
    int Priority = 0,
    DateOnly? EffectiveTo = null) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record DeactivateDeductionCommand(
    Guid EmployeeId,
    Guid DeductionId,
    DateOnly EffectiveTo) : IRequest<EmployeeDetailDto>, ITransactionalRequest;

public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeDetailDto?>;

public record GetEmployeesQuery(
    string? Search = null,
    EmploymentStatus? Status = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<EmployeeListItemDto>>;
