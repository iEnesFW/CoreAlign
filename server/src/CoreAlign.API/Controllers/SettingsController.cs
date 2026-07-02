using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Settings;
using CoreAlign.Application.Settings.Commands;
using CoreAlign.Application.Settings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SettingsController(IMediator mediator) => _mediator = mediator;

    // ---------- Company profile ----------

    [HttpGet("company")]
    public async Task<IActionResult> GetCompanyProfile(CancellationToken ct)
        => (await _mediator.Send(new GetCompanyProfileQuery(), ct)).ToOk();

    [HttpPut("company")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateCompanyProfile([FromBody] UpdateCompanyProfileCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToOk();

    // ---------- Parameters (TenantSetting key/value store) ----------

    [HttpGet("parameters")]
    public async Task<IActionResult> GetParameters([FromQuery] string? category, CancellationToken ct)
        => (await _mediator.Send(new GetTenantSettingsQuery(category), ct)).ToOk();

    [HttpPut("parameters")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpsertParameters([FromBody] UpsertTenantSettingsCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToOk();

    [HttpDelete("parameters/{category}/{key}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteParameter(string category, string key, CancellationToken ct)
        => (await _mediator.Send(new DeleteTenantSettingCommand(category, key), ct)).ToOk();

    // ---------- Email templates ----------

    [HttpGet("email-templates")]
    public async Task<IActionResult> GetEmailTemplates(CancellationToken ct)
        => (await _mediator.Send(new GetEmailTemplatesQuery(), ct)).ToOk();

    [HttpGet("email-templates/{id:guid}")]
    public async Task<IActionResult> GetEmailTemplate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetEmailTemplateByIdQuery(id), ct)).ToOk();

    [HttpPost("email-templates")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> CreateEmailTemplate([FromBody] CreateEmailTemplateCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPut("email-templates/{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateEmailTemplate(Guid id, [FromBody] UpdateEmailTemplateCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id) return BadRequest(ApiResponse<object>.Failure("Route id mismatch.", 400));
        return (await _mediator.Send(cmd, ct)).ToOk();
    }

    [HttpDelete("email-templates/{id:guid}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> DeleteEmailTemplate(Guid id, CancellationToken ct)
        => (await _mediator.Send(new DeleteEmailTemplateCommand(id), ct)).ToOk();

    // ---------- Document number sequences ----------

    [HttpGet("document-sequences")]
    public async Task<IActionResult> GetDocumentSequences(CancellationToken ct)
        => (await _mediator.Send(new ListDocumentSequencesQuery(), ct)).ToOk();

    [HttpPost("document-sequences")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> ConfigureDocumentSequence([FromBody] ConfigureDocumentSequenceCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToOk();
}
