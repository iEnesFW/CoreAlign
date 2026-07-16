using CoreAlign.Application.Manufacturing.Commands;
using CoreAlign.Application.Manufacturing.DTOs;
using CoreAlign.Application.Manufacturing.Mapping;
using CoreAlign.Application.Manufacturing.Queries;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Manufacturing.Handlers;

internal static class OperatorDtoAssembler
{
    public static WorkCenterOperatorDto Build(WorkCenterOperator op, WorkCenter? wc, Employee? emp) =>
        new(
            op.Id,
            op.WorkCenterId,
            wc?.Code ?? string.Empty,
            wc?.Name ?? string.Empty,
            op.EmployeeId,
            emp?.FullName ?? string.Empty,
            emp is not null && !emp.IsDeleted && emp.Status != EmploymentStatus.Terminated,
            op.QualificationLevel,
            op.IsPrimary,
            op.IsActive,
            op.CertifiedOn,
            op.Notes);
}

public class CreateWorkCenterOperatorHandler
    : IRequestHandler<CreateWorkCenterOperatorCommand, WorkCenterOperatorDto>
{
    private readonly IWorkCenterOperatorRepository _operators;
    private readonly IWorkCenterRepository _workCenters;
    private readonly IEmployeeRepository _employees;
    private readonly ITenantContext _tenant;

    public CreateWorkCenterOperatorHandler(
        IWorkCenterOperatorRepository operators,
        IWorkCenterRepository workCenters,
        IEmployeeRepository employees,
        ITenantContext tenant)
    {
        _operators = operators;
        _workCenters = workCenters;
        _employees = employees;
        _tenant = tenant;
    }

    public async Task<WorkCenterOperatorDto> Handle(CreateWorkCenterOperatorCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();

        var wc = (await _workCenters.GetByIdsAsync(new[] { c.WorkCenterId }, ct)).FirstOrDefault();
        if (wc is null || !wc.IsActive)
        {
            throw new WorkCenterNotFoundException(c.WorkCenterId);
        }

        var emp = await _employees.GetByIdAsync(c.EmployeeId, ct);
        if (emp is null || emp.IsDeleted || emp.Status == EmploymentStatus.Terminated)
        {
            throw new EmployeeNotFoundException();
        }

        if (await _operators.ActiveAssignmentExistsAsync(tenantId, c.WorkCenterId, c.EmployeeId, null, ct))
        {
            throw new WorkCenterOperatorAlreadyAssignedException();
        }

        var op = new WorkCenterOperator(
            c.WorkCenterId, c.EmployeeId, c.QualificationLevel, c.IsPrimary, c.CertifiedOn, c.Notes);
        await _operators.AddAsync(op, ct);

        return OperatorDtoAssembler.Build(op, wc, emp);
    }
}

public class UpdateWorkCenterOperatorHandler
    : IRequestHandler<UpdateWorkCenterOperatorCommand, WorkCenterOperatorDto>
{
    private readonly IWorkCenterOperatorRepository _operators;
    private readonly IWorkCenterRepository _workCenters;
    private readonly IEmployeeRepository _employees;
    private readonly ITenantContext _tenant;

    public UpdateWorkCenterOperatorHandler(
        IWorkCenterOperatorRepository operators,
        IWorkCenterRepository workCenters,
        IEmployeeRepository employees,
        ITenantContext tenant)
    {
        _operators = operators;
        _workCenters = workCenters;
        _employees = employees;
        _tenant = tenant;
    }

    public async Task<WorkCenterOperatorDto> Handle(UpdateWorkCenterOperatorCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var op = await _operators.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new WorkCenterOperatorNotFoundException(c.Id);

        op.Update(c.QualificationLevel, c.IsPrimary, c.IsActive, c.CertifiedOn, c.Notes);

        var wc = (await _workCenters.GetByIdsAsync(new[] { op.WorkCenterId }, ct)).FirstOrDefault();
        var emp = await _employees.GetByIdAsync(op.EmployeeId, ct);
        return OperatorDtoAssembler.Build(op, wc, emp);
    }
}

public class DeactivateWorkCenterOperatorHandler
    : IRequestHandler<DeactivateWorkCenterOperatorCommand, Unit>
{
    private readonly IWorkCenterOperatorRepository _operators;
    private readonly ITenantContext _tenant;

    public DeactivateWorkCenterOperatorHandler(IWorkCenterOperatorRepository operators, ITenantContext tenant)
    {
        _operators = operators;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(DeactivateWorkCenterOperatorCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var op = await _operators.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new WorkCenterOperatorNotFoundException(c.Id);
        op.Deactivate();
        return Unit.Value;
    }
}

public class ActivateWorkCenterOperatorHandler
    : IRequestHandler<ActivateWorkCenterOperatorCommand, Unit>
{
    private readonly IWorkCenterOperatorRepository _operators;
    private readonly ITenantContext _tenant;

    public ActivateWorkCenterOperatorHandler(IWorkCenterOperatorRepository operators, ITenantContext tenant)
    {
        _operators = operators;
        _tenant = tenant;
    }

    public async Task<Unit> Handle(ActivateWorkCenterOperatorCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var op = await _operators.GetByIdAsync(tenantId, c.Id, ct)
            ?? throw new WorkCenterOperatorNotFoundException(c.Id);
        op.Activate();
        return Unit.Value;
    }
}

public class GetWorkCenterOperatorByIdHandler
    : IRequestHandler<GetWorkCenterOperatorByIdQuery, WorkCenterOperatorDto>
{
    private readonly IWorkCenterOperatorRepository _operators;
    private readonly ITenantContext _tenant;

    public GetWorkCenterOperatorByIdHandler(IWorkCenterOperatorRepository operators, ITenantContext tenant)
    {
        _operators = operators;
        _tenant = tenant;
    }

    public async Task<WorkCenterOperatorDto> Handle(GetWorkCenterOperatorByIdQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var row = await _operators.GetRowByIdAsync(tenantId, q.Id, ct)
            ?? throw new WorkCenterOperatorNotFoundException(q.Id);
        return RoutingMapper.ToDto(row);
    }
}

public class ListWorkCenterOperatorsHandler
    : IRequestHandler<ListWorkCenterOperatorsQuery, IReadOnlyList<WorkCenterOperatorDto>>
{
    private readonly IWorkCenterOperatorRepository _operators;
    private readonly ITenantContext _tenant;

    public ListWorkCenterOperatorsHandler(IWorkCenterOperatorRepository operators, ITenantContext tenant)
    {
        _operators = operators;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<WorkCenterOperatorDto>> Handle(
        ListWorkCenterOperatorsQuery q, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        var rows = await _operators.ListAsync(
            tenantId, q.WorkCenterId, q.EmployeeId, Math.Clamp(q.Take, 1, 500), ct);
        return rows.Select(RoutingMapper.ToDto).ToList();
    }
}
