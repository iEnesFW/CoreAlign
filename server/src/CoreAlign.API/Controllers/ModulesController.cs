using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Billing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/modules")]
public class ModulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ModulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken)
        => (await _mediator.Send(new ListModulesCatalogQuery(), cancellationToken)).ToOk();

    [HttpGet("active")]
    public async Task<IActionResult> Active(CancellationToken cancellationToken)
        => (await _mediator.Send(new ListTenantModulesQuery(), cancellationToken)).ToOk();
}
