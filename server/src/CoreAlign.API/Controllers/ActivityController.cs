using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Activity.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = "TenantAdmin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActivityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetActivityLogsQuery(page, pageSize), cancellationToken);
        return result.ToOk();
    }
}
