using Asp.Versioning;
using CoreAlign.API.Authorization;
using CoreAlign.API.Common;
using CoreAlign.Application.Reports.Common;
using CoreAlign.Application.Reports.Custom;
using CoreAlign.Domain.Entities.Reporting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/custom")]
public sealed class CustomReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFieldCatalogService _catalog;
    private readonly IReportFileFactory _files;

    public CustomReportsController(IMediator mediator, IFieldCatalogService catalog, IReportFileFactory files)
    {
        _mediator = mediator;
        _catalog = catalog;
        _files = files;
    }

    [HttpGet("catalog")]
    public IActionResult GetCatalog() => _catalog.GetCatalog().ToOk();

    [HttpGet("catalog/{entityType}")]
    public IActionResult GetCatalogForEntity(ReportEntityType entityType)
    {
        var group = _catalog.Get(entityType);
        return group is null ? NotFound() : group.ToOk();
    }

    [HttpPost("preview")]
    public async Task<IActionResult> PreviewAsync([FromBody] CustomReportDefinitionDto definition, CancellationToken cancellationToken)
    {
        try
        {
            var preview = await _mediator.Send(new PreviewCustomReportQuery(definition), cancellationToken);
            return preview.ToOk();
        }
        catch (CustomReportValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _mediator.Send(new ListCustomReportsQuery(), cancellationToken);
        return rows.ToOk();
    }

    [HttpPost]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
    public async Task<IActionResult> SaveAsync([FromBody] SaveCustomReportRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _mediator.Send(new SaveCustomReportCommand(request), cancellationToken);
            return summary.ToCreated();
        }
        catch (CustomReportValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = PersonaPolicies.TenantAdminRole + "," + PersonaPolicies.PlatformAdminRole)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteCustomReportCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/run")]
    public async Task<IActionResult> RunAsync(
        Guid id,
        [FromQuery] string format = "pdf",
        CancellationToken cancellationToken = default)
    {
        var document = await _mediator.Send(new RunCustomReportQuery(id), cancellationToken);
        if (document is null) return NotFound();
        var fmt = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ? ReportFormat.Xlsx : ReportFormat.Pdf;
        var file = await _files.RenderAsync(document, fmt, $"custom-{id}", cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
