using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Payroll.Employees;

internal static class EmployeeMapper
{
    public static EmployeeDetailDto ToDetailDto(Employee e) => new(
        e.Id,
        e.EmployeeNumber,
        e.FirstName,
        e.LastName,
        e.FullName,
        PiiMasking.MaskNationalId(e.NationalId),
        e.SgkRegistrationNo,
        e.Email,
        e.Phone,
        e.HireDate,
        e.TerminationDate,
        e.Status,
        e.Department,
        e.Title,
        e.EmploymentType,
        e.SalaryBasis,
        e.BaseSalaryGross,
        e.SalaryCurrency,
        PiiMasking.MaskIban(e.Iban),
        e.BankName,
        e.IsSgkIncentiveEligible,
        e.DisabilityDegree,
        e.IsRetiredWorking,
        e.SgkExempt,
        e.DependentCount,
        e.SpouseEmployed,
        e.TerminationReason,
        e.SalaryComponents.OrderBy(c => c.EffectiveFrom).ThenBy(c => c.ComponentType).Select(ToDto).ToList(),
        e.Deductions.OrderBy(d => d.Priority).ThenBy(d => d.EffectiveFrom).Select(ToDto).ToList(),
        e.CreatedAtUtc);

    public static EmployeeListItemDto ToListItemDto(Employee e) => new(
        e.Id,
        e.EmployeeNumber,
        e.FirstName,
        e.LastName,
        e.FullName,
        PiiMasking.MaskNationalId(e.NationalId),
        e.Status,
        e.EmploymentType,
        e.Department,
        e.Title,
        e.HireDate,
        e.TerminationDate,
        e.BaseSalaryGross,
        e.SalaryCurrency,
        PiiMasking.MaskIban(e.Iban));

    private static SalaryComponentDto ToDto(SalaryComponent c) => new(
        c.Id, c.ComponentType, c.Amount, c.IsRecurring, c.TaxExempt, c.SgkExempt,
        c.EffectiveFrom, c.EffectiveTo, c.IsActive);

    private static EmployeeDeductionDto ToDto(EmployeeDeduction d) => new(
        d.Id, d.DeductionType, d.Amount, d.Percent, d.RemainingBalance, d.Priority,
        d.EffectiveFrom, d.EffectiveTo, d.IsActive);
}

public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IDocumentSequenceRepository _sequences;
    private readonly IUnitOfWork _uow;

    public CreateEmployeeHandler(IEmployeeRepository employees, IDocumentSequenceRepository sequences, IUnitOfWork uow)
    {
        _employees = employees;
        _sequences = sequences;
        _uow = uow;
    }

    public async Task<EmployeeDetailDto> Handle(CreateEmployeeCommand c, CancellationToken ct)
    {
        var nationalId = c.NationalId.Trim();
        if (await _employees.NationalIdExistsAsync(nationalId, null, ct))
        {
            throw new DuplicateEmployeeNationalIdException();
        }

        var now = DateTime.UtcNow;
        var seq = await _sequences.GetAsync(DocumentSequenceType.EmployeeNumber, ct);
        string employeeNumber;
        if (seq is null)
        {
            seq = new DocumentSequence(DocumentSequenceType.EmployeeNumber, "PER", now.Year, 1, 5);
            employeeNumber = seq.ConsumeNext(now);
            await _sequences.AddAsync(seq, ct);
        }
        else
        {
            employeeNumber = seq.ConsumeNext(now);
            _sequences.Update(seq);
        }

        var employee = new Employee(
            employeeNumber,
            c.FirstName,
            c.LastName,
            nationalId,
            c.HireDate,
            c.BaseSalaryGross,
            c.EmploymentType,
            c.SalaryBasis,
            c.SalaryCurrency.ToUpperInvariant(),
            c.SgkRegistrationNo,
            c.Email,
            c.Phone,
            c.Department,
            c.Title,
            c.Iban,
            c.BankName,
            c.IsSgkIncentiveEligible,
            c.DisabilityDegree,
            c.IsRetiredWorking,
            c.SgkExempt,
            c.DependentCount,
            c.SpouseEmployed,
            c.UserId);

        await _employees.AddAsync(employee, ct);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public UpdateEmployeeHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(UpdateEmployeeCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.Id, ct) ?? throw new EmployeeNotFoundException();
        employee.UpdateProfile(
            c.FirstName, c.LastName, c.Email, c.Phone, c.Department, c.Title,
            c.Iban, c.BankName, c.DependentCount, c.SpouseEmployed);
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class ChangeBaseSalaryHandler : IRequestHandler<ChangeBaseSalaryCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public ChangeBaseSalaryHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(ChangeBaseSalaryCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.Id, ct) ?? throw new EmployeeNotFoundException();
        employee.ChangeBaseSalary(c.BaseSalaryGross, c.EffectiveDate);
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class PlaceEmployeeOnLeaveHandler : IRequestHandler<PlaceEmployeeOnLeaveCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public PlaceEmployeeOnLeaveHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(PlaceEmployeeOnLeaveCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.Id, ct) ?? throw new EmployeeNotFoundException();
        employee.PlaceOnLeave();
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class ReturnEmployeeFromLeaveHandler : IRequestHandler<ReturnEmployeeFromLeaveCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public ReturnEmployeeFromLeaveHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(ReturnEmployeeFromLeaveCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.Id, ct) ?? throw new EmployeeNotFoundException();
        employee.ReturnFromLeave();
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class TerminateEmployeeHandler : IRequestHandler<TerminateEmployeeCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public TerminateEmployeeHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(TerminateEmployeeCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.Id, ct) ?? throw new EmployeeNotFoundException();
        employee.Terminate(c.TerminationDate, c.Reason);
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class AddSalaryComponentHandler : IRequestHandler<AddSalaryComponentCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public AddSalaryComponentHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(AddSalaryComponentCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.EmployeeId, ct) ?? throw new EmployeeNotFoundException();
        employee.AddSalaryComponent(new SalaryComponent(
            c.ComponentType, c.Amount, c.EffectiveFrom, c.IsRecurring, c.TaxExempt, c.SgkExempt, c.EffectiveTo));
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class UpdateSalaryComponentHandler : IRequestHandler<UpdateSalaryComponentCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public UpdateSalaryComponentHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(UpdateSalaryComponentCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.EmployeeId, ct) ?? throw new EmployeeNotFoundException();
        var component = employee.SalaryComponents.FirstOrDefault(x => x.Id == c.ComponentId)
            ?? throw new SalaryComponentNotFoundException();
        component.Update(c.Amount, c.EffectiveFrom, c.IsRecurring, c.TaxExempt, c.SgkExempt, c.EffectiveTo);
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class DeactivateSalaryComponentHandler : IRequestHandler<DeactivateSalaryComponentCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public DeactivateSalaryComponentHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(DeactivateSalaryComponentCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.EmployeeId, ct) ?? throw new EmployeeNotFoundException();
        var component = employee.SalaryComponents.FirstOrDefault(x => x.Id == c.ComponentId)
            ?? throw new SalaryComponentNotFoundException();
        component.Deactivate(c.EffectiveTo);
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class AddDeductionHandler : IRequestHandler<AddDeductionCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public AddDeductionHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(AddDeductionCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.EmployeeId, ct) ?? throw new EmployeeNotFoundException();
        employee.AddDeduction(new EmployeeDeduction(
            c.DeductionType, c.EffectiveFrom, c.Amount, c.Percent, c.RemainingBalance, c.Priority, c.EffectiveTo));
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class UpdateDeductionHandler : IRequestHandler<UpdateDeductionCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public UpdateDeductionHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(UpdateDeductionCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.EmployeeId, ct) ?? throw new EmployeeNotFoundException();
        var deduction = employee.Deductions.FirstOrDefault(x => x.Id == c.DeductionId)
            ?? throw new EmployeeDeductionNotFoundException();
        deduction.Update(c.EffectiveFrom, c.Amount, c.Percent, c.RemainingBalance, c.Priority, c.EffectiveTo);
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class DeactivateDeductionHandler : IRequestHandler<DeactivateDeductionCommand, EmployeeDetailDto>
{
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    public DeactivateDeductionHandler(IEmployeeRepository employees, IUnitOfWork uow) { _employees = employees; _uow = uow; }

    public async Task<EmployeeDetailDto> Handle(DeactivateDeductionCommand c, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(c.EmployeeId, ct) ?? throw new EmployeeNotFoundException();
        var deduction = employee.Deductions.FirstOrDefault(x => x.Id == c.DeductionId)
            ?? throw new EmployeeDeductionNotFoundException();
        deduction.Deactivate(c.EffectiveTo);
        _employees.Update(employee);
        await _uow.SaveChangesAsync(ct);
        return EmployeeMapper.ToDetailDto(employee);
    }
}

public class GetEmployeeByIdHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDetailDto?>
{
    private readonly IEmployeeRepository _employees;
    public GetEmployeeByIdHandler(IEmployeeRepository employees) => _employees = employees;

    public async Task<EmployeeDetailDto?> Handle(GetEmployeeByIdQuery q, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(q.Id, ct);
        return employee is null ? null : EmployeeMapper.ToDetailDto(employee);
    }
}

public class GetEmployeesHandler : IRequestHandler<GetEmployeesQuery, PagedResult<EmployeeListItemDto>>
{
    private readonly IEmployeeRepository _employees;
    public GetEmployeesHandler(IEmployeeRepository employees) => _employees = employees;

    public async Task<PagedResult<EmployeeListItemDto>> Handle(GetEmployeesQuery q, CancellationToken ct)
    {
        var page = q.Page < 1 ? 1 : q.Page;
        var pageSize = q.PageSize is < 1 or > 200 ? 25 : q.PageSize;
        var (items, total) = await _employees.GetPagedAsync(q.Search, q.Status, page, pageSize, ct);
        return new PagedResult<EmployeeListItemDto>
        {
            Items = items.Select(EmployeeMapper.ToListItemDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
