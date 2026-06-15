using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Installation;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/installation-acceptances")]
public class InstallationAcceptanceController : ControllerBase
{
    private readonly IMediator _mediator;
    public InstallationAcceptanceController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> ListByWorkOrder([FromQuery] Guid workOrderId, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetInstallationAcceptanceByWorkOrderIdQuery(workOrderId), ct);
        return dto is null
            ? Array.Empty<InstallationAcceptanceDto>().ToOk()
            : new[] { dto }.ToOk();
    }

    [HttpGet("inspector/{inspectorUserId:guid}")]
    public async Task<IActionResult> ListForInspector(
        Guid inspectorUserId,
        [FromQuery] InstallationAcceptanceStatus? status,
        CancellationToken ct)
        => (await _mediator.Send(new ListPendingAcceptancesForInspectorQuery(inspectorUserId, status), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetAcceptanceWithFullDetailsQuery(id), ct)).ToOk();

    [HttpGet("punch-list")]
    public async Task<IActionResult> ListPunch([FromQuery] PunchListItemStatus status, CancellationToken ct)
        => (await _mediator.Send(new ListPunchListItemsQuery(status), ct)).ToOk();

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartInstallationAcceptanceCommand cmd, CancellationToken ct)
        => (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPatch("{id:guid}/checklist")]
    public async Task<IActionResult> UpdateChecklist(Guid id, [FromBody] UpdateChecklistItemCommand cmd, CancellationToken ct)
        => id != cmd.AcceptanceId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/photos")]
    public async Task<IActionResult> AddPhoto(Guid id, [FromBody] UploadAcceptancePhotoCommand cmd, CancellationToken ct)
        => id != cmd.AcceptanceId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/signature")]
    public async Task<IActionResult> CaptureSignature(Guid id, [FromBody] CaptureCustomerSignatureCommand cmd, CancellationToken ct)
        => id != cmd.AcceptanceId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptInstallationCommand cmd, CancellationToken ct)
        => id != cmd.AcceptanceId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectInstallationCommand cmd, CancellationToken ct)
        => id != cmd.AcceptanceId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();

    [HttpPost("{id:guid}/punch-list")]
    public async Task<IActionResult> AddPunchListItem(Guid id, [FromBody] AddPunchListItemCommand cmd, CancellationToken ct)
        => id != cmd.AcceptanceId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToCreated();

    [HttpPost("punch-list/{punchItemId:guid}/resolve")]
    public async Task<IActionResult> ResolvePunchListItem(Guid punchItemId, [FromBody] ResolvePunchListItemCommand cmd, CancellationToken ct)
        => punchItemId != cmd.PunchItemId ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();
}
