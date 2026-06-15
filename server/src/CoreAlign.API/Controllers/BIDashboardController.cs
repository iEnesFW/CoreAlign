using Asp.Versioning;
using CoreAlign.Application.B2B;
using CoreAlign.Application.BI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/bi/dashboard")]
public sealed class BIDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly ICurrentUserAccessor _user;

    public BIDashboardController(IDashboardService dashboard, ICurrentUserAccessor user)
    {
        _dashboard = dashboard;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        var widgets = await _dashboard.GetUserDashboardAsync(userId, cancellationToken);
        return Ok(widgets);
    }

    [HttpPut]
    public async Task<IActionResult> SaveLayoutAsync([FromBody] List<DashboardWidgetUpsertDto> widgets, CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        await _dashboard.SaveWidgetLayoutAsync(userId, widgets, cancellationToken);
        return NoContent();
    }

    [HttpPost("widgets")]
    public async Task<IActionResult> AddWidgetAsync([FromBody] DashboardWidgetUpsertDto widget, CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        var created = await _dashboard.AddWidgetAsync(userId, widget, cancellationToken);
        return Ok(created);
    }

    [HttpDelete("widgets/{id:guid}")]
    public async Task<IActionResult> RemoveWidgetAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        await _dashboard.RemoveWidgetAsync(userId, id, cancellationToken);
        return NoContent();
    }
}
