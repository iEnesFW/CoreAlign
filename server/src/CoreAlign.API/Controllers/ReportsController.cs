using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("sales-by-period")]
    public async Task<IActionResult> GetSalesByPeriodAsync(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] SalesBucket bucket = SalesBucket.Month,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetSalesByPeriodQuery(fromUtc, toUtc, bucket), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("top-customers")]
    public async Task<IActionResult> GetTopCustomersAsync(
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTopCustomersQuery(limit, fromUtc, toUtc), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProductsAsync(
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTopProductsQuery(limit, fromUtc, toUtc), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("aging-summary")]
    public async Task<IActionResult> GetAgingSummaryAsync(
        [FromQuery] DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAgingSummaryQuery(asOfUtc), cancellationToken);
        return result.ToOk();
    }
}
