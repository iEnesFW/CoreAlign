using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Reports.Schedules;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/schedules")]
public sealed class ReportSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportSchedulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _mediator.Send(new ListReportSchedulesQuery(), cancellationToken);
        return rows.ToOk();
    }

    [HttpPost]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateReportScheduleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _mediator.Send(new CreateReportScheduleCommand(request), cancellationToken);
            return created.ToCreated();
        }
        catch (ScheduleValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateReportScheduleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _mediator.Send(new UpdateReportScheduleCommand(id, request), cancellationToken);
            return updated is null ? NotFound() : updated.ToOk();
        }
        catch (ScheduleValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteReportScheduleCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
