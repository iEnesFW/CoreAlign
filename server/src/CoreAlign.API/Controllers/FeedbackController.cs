using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Feedback;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;
    public FeedbackController(IMediator mediator) => _mediator = mediator;

    private static IActionResult RouteIdMismatch() =>
        new BadRequestObjectResult(ApiResponse<object>.Failure("Route id does not match command id.", 400));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] FeedbackStatus? status, [FromQuery] FeedbackType? type, CancellationToken ct)
        => (await _mediator.Send(new ListFeedbackQuery(status, type), ct)).ToOk();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _mediator.Send(new GetFeedbackByIdQuery(id), ct)).ToOk();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackCommand cmd, CancellationToken ct)
    {
        // The submitter's name is taken from the authenticated identity, never the
        // request body, so it cannot be spoofed.
        var withUser = cmd with { CreatedByName = User.Identity?.Name };
        return (await _mediator.Send(withUser, ct)).ToCreated();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateFeedbackStatusCommand cmd, CancellationToken ct)
        => id != cmd.Id ? RouteIdMismatch() : (await _mediator.Send(cmd, ct)).ToOk();
}
