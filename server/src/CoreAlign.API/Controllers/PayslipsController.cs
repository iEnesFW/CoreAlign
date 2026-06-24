using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Payroll.Runs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payslips")]
public class PayslipsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PayslipsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetPayslipByIdQuery(id), ct)).ToOk();
}
