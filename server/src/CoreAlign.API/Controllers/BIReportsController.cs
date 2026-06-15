using Asp.Versioning;
using CoreAlign.Application.B2B;
using CoreAlign.Application.BI;
using CoreAlign.Domain.Entities.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/bi/reports")]
public sealed class BIReportsController : ControllerBase
{
    private readonly ISavedReportService _saved;
    private readonly IBIReportService _bi;
    private readonly ICurrentUserAccessor _user;

    public BIReportsController(ISavedReportService saved, IBIReportService bi, ICurrentUserAccessor user)
    {
        _saved = saved;
        _bi = bi;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        var rows = await _saved.ListAsync(userId, cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] SavedReportUpsertDto dto, CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        var created = await _saved.CreateAsync(userId, dto, cancellationToken);
        return Ok(created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] SavedReportUpsertDto dto, CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        var updated = await _saved.UpdateAsync(userId, id, dto, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _user.UserIdOrThrow();
        await _saved.DeleteAsync(userId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/run")]
    public async Task<IActionResult> RunAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bi.RunSavedReportAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/export")]
    public async Task<IActionResult> ExportAsync(Guid id, [FromQuery] BIExportFormat format, CancellationToken cancellationToken)
    {
        var bytes = await _bi.ExportAsync(id, format, cancellationToken);
        var (contentType, ext) = format switch
        {
            BIExportFormat.Pdf => ("application/pdf", "pdf"),
            BIExportFormat.Xlsx => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            BIExportFormat.Csv => ("text/csv; charset=utf-8", "csv"),
            _ => ("application/octet-stream", "bin"),
        };
        return File(bytes, contentType, $"report-{id}.{ext}");
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteAdHocAsync(
        [FromQuery] BIDataSource dataSource,
        [FromBody] BIQueryConfigDto config,
        CancellationToken cancellationToken)
    {
        var result = await _bi.ExecuteAsync(dataSource, config, cancellationToken);
        return Ok(result);
    }
}
