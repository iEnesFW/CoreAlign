using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Payroll.GL;
using CoreAlign.Application.Payroll.Runs;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll-runs")]
public class PayrollRunsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayrollRunsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int? periodYear,
        [FromQuery] PayrollRunStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => (await _mediator.Send(new GetPayrollRunsQuery(periodYear, status, page, pageSize), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPayrollRunByIdQuery(id), ct)).ToOk();

    [HttpGet("{id:guid}/payslips")]
    public async Task<IActionResult> GetPayslips(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPayslipsByRunQuery(id), ct)).ToOk();

    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePayrollRunCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("{id:guid}/calculate")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Calculate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new CalculatePayrollRunCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ApprovePayrollRunCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct)
        => (await _mediator.Send(new ReopenPayrollRunCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/post")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Post(Guid id, CancellationToken ct)
        => (await _mediator.Send(new PostPayrollRunCommand(id), ct)).ToOk();

    [HttpPost("{id:guid}/pay")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> Pay(Guid id, CancellationToken ct)
        => (await _mediator.Send(new PayPayrollRunCommand(id), ct)).ToOk();

    [HttpPost("pay-taxes")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> PayTaxes([FromBody] PayPayrollTaxesCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("pay-sgk")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> PaySgk([FromBody] PayPayrollSgkCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToOk();
}
