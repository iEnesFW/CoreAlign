using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Payroll.Parameters;

internal static class PayrollParametersMapper
{
    public static PayrollParametersDto ToDto(PayrollParameters p) => new(
        p.Id,
        p.TenantId,
        p.TenantId == Guid.Empty,
        p.EffectiveYear,
        p.EffectiveFrom,
        p.EffectiveTo,
        p.IsActive,
        p.Description,
        p.SgkEmployeeRate,
        p.SgkEmployerRate,
        p.SgkEmployer5PointIncentiveRate,
        p.UnemploymentEmployeeRate,
        p.UnemploymentEmployerRate,
        p.SgkFloorMonthly,
        p.SgkCeilingMultiplier,
        p.SgkCeilingMonthly,
        p.StampTaxRate,
        p.GrossMinimumWage,
        p.MinWageExemptionEnabled,
        p.Disability1Amount,
        p.Disability2Amount,
        p.Disability3Amount,
        p.TaxBrackets
            .OrderBy(b => b.SortOrder)
            .Select(b => new PayrollTaxBracketDto(b.Id, b.RatePercent, b.SortOrder, b.UpperBound))
            .ToList(),
        p.CreatedAtUtc,
        p.UpdatedAtUtc);
}

public class CreatePayrollParametersHandler : IRequestHandler<CreatePayrollParametersCommand, PayrollParametersDto>
{
    private readonly IPayrollParametersRepository _parameters;
    private readonly IUnitOfWork _uow;

    public CreatePayrollParametersHandler(IPayrollParametersRepository parameters, IUnitOfWork uow)
    {
        _parameters = parameters;
        _uow = uow;
    }

    public async Task<PayrollParametersDto> Handle(CreatePayrollParametersCommand c, CancellationToken ct)
    {
        var parameters = new PayrollParameters(
            c.EffectiveYear,
            c.EffectiveFrom,
            c.SgkEmployeeRate,
            c.SgkEmployerRate,
            c.SgkEmployer5PointIncentiveRate,
            c.UnemploymentEmployeeRate,
            c.UnemploymentEmployerRate,
            c.SgkFloorMonthly,
            c.SgkCeilingMultiplier,
            c.SgkCeilingMonthly,
            c.StampTaxRate,
            c.GrossMinimumWage,
            c.Disability1Amount,
            c.Disability2Amount,
            c.Disability3Amount,
            c.MinWageExemptionEnabled,
            c.EffectiveTo,
            c.Description);

        foreach (var bracket in c.TaxBrackets)
        {
            parameters.AddTaxBracket(new PayrollTaxBracket(bracket.RatePercent, bracket.SortOrder, bracket.UpperBound));
        }

        await _parameters.AddAsync(parameters, ct);
        await _uow.SaveChangesAsync(ct);
        return PayrollParametersMapper.ToDto(parameters);
    }
}

public class UpdatePayrollParametersHandler : IRequestHandler<UpdatePayrollParametersCommand, PayrollParametersDto>
{
    private readonly IPayrollParametersRepository _parameters;
    private readonly IUnitOfWork _uow;

    public UpdatePayrollParametersHandler(IPayrollParametersRepository parameters, IUnitOfWork uow)
    {
        _parameters = parameters;
        _uow = uow;
    }

    public async Task<PayrollParametersDto> Handle(UpdatePayrollParametersCommand c, CancellationToken ct)
    {
        var parameters = await _parameters.GetOwnedByIdAsync(c.Id, ct)
            ?? throw new PayrollParametersNotFoundException(c.Id);
        if (parameters.TenantId == Guid.Empty)
        {
            throw new GlobalPayrollParametersReadOnlyException();
        }

        parameters.Update(
            c.SgkEmployeeRate,
            c.SgkEmployerRate,
            c.SgkEmployer5PointIncentiveRate,
            c.UnemploymentEmployeeRate,
            c.UnemploymentEmployerRate,
            c.SgkFloorMonthly,
            c.SgkCeilingMultiplier,
            c.SgkCeilingMonthly,
            c.StampTaxRate,
            c.GrossMinimumWage,
            c.Disability1Amount,
            c.Disability2Amount,
            c.Disability3Amount,
            c.MinWageExemptionEnabled,
            c.IsActive,
            c.EffectiveFrom,
            c.EffectiveTo,
            c.Description);

        _parameters.Update(parameters);
        await _uow.SaveChangesAsync(ct);
        return PayrollParametersMapper.ToDto(parameters);
    }
}

public class GetPayrollParametersListHandler : IRequestHandler<GetPayrollParametersListQuery, IReadOnlyList<PayrollParametersDto>>
{
    private readonly IPayrollParametersRepository _parameters;
    public GetPayrollParametersListHandler(IPayrollParametersRepository parameters) => _parameters = parameters;

    public async Task<IReadOnlyList<PayrollParametersDto>> Handle(GetPayrollParametersListQuery q, CancellationToken ct)
    {
        var rows = await _parameters.ListAsync(q.Year, ct);
        return rows.Select(PayrollParametersMapper.ToDto).ToList();
    }
}

public class GetPayrollParametersByIdHandler : IRequestHandler<GetPayrollParametersByIdQuery, PayrollParametersDto?>
{
    private readonly IPayrollParametersRepository _parameters;
    public GetPayrollParametersByIdHandler(IPayrollParametersRepository parameters) => _parameters = parameters;

    public async Task<PayrollParametersDto?> Handle(GetPayrollParametersByIdQuery q, CancellationToken ct)
    {
        var parameters = await _parameters.GetByIdAsync(q.Id, ct);
        return parameters is null ? null : PayrollParametersMapper.ToDto(parameters);
    }
}
