using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Payroll.Employees;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;
    public EmployeesController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] EmploymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetEmployeesQuery(search, status, page, pageSize), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetEmployeeByIdQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPut("{id:guid}/base-salary")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ChangeBaseSalary(Guid id, [FromBody] ChangeBaseSalaryCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/terminate")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Terminate(Guid id, [FromBody] TerminateEmployeeCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/leave")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> PlaceOnLeave(Guid id, CancellationToken ct)
        => (await _mediator.Send(new PlaceEmployeeOnLeaveCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/return-from-leave")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ReturnFromLeave(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ReturnEmployeeFromLeaveCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/components")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> AddComponent(Guid id, [FromBody] AddSalaryComponentCommand cmd, CancellationToken ct)
        => id != cmd.EmployeeId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}/components/{componentId:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateComponent(Guid id, Guid componentId, [FromBody] UpdateSalaryComponentCommand cmd, CancellationToken ct)
        => id != cmd.EmployeeId || componentId != cmd.ComponentId
            ? RouteIdMismatch()
            : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("{id:guid}/components/{componentId:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeactivateComponent(Guid id, Guid componentId, [FromBody] DeactivateSalaryComponentCommand cmd, CancellationToken ct)
        => id != cmd.EmployeeId || componentId != cmd.ComponentId
            ? RouteIdMismatch()
            : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/deductions")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> AddDeduction(Guid id, [FromBody] AddDeductionCommand cmd, CancellationToken ct)
        => id != cmd.EmployeeId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("{id:guid}/deductions/{deductionId:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateDeduction(Guid id, Guid deductionId, [FromBody] UpdateDeductionCommand cmd, CancellationToken ct)
        => id != cmd.EmployeeId || deductionId != cmd.DeductionId
            ? RouteIdMismatch()
            : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("{id:guid}/deductions/{deductionId:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeactivateDeduction(Guid id, Guid deductionId, [FromBody] DeactivateDeductionCommand cmd, CancellationToken ct)
        => id != cmd.EmployeeId || deductionId != cmd.DeductionId
            ? RouteIdMismatch()
            : (await _mediator.Send(cmd, ct)).ToOk();
}
