using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Manufacturing.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/manufacturing/dashboard")]
public class ManufacturingDashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public ManufacturingDashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis([FromQuery] DateTime startDateUtc, [FromQuery] DateTime endDateUtc, CancellationToken ct)
    {
        var kpis = await _mediator.Send(new GetManufacturingKpiSummaryQuery(startDateUtc, endDateUtc), ct);
        return Ok(kpis);
    }
}
