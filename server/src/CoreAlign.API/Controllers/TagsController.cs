using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Tags.Commands;
using CoreAlign.Application.Tags.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TagsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TagsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> ListTagsAsync([FromQuery] bool? isActive, CancellationToken cancellationToken)
        => (await _mediator.Send(new ListTagsQuery(isActive), cancellationToken)).ToOk();

    [HttpPost]
    public async Task<IActionResult> CreateTagAsync([FromBody] CreateTagCommand command, CancellationToken cancellationToken)
        => (await _mediator.Send(command, cancellationToken)).ToCreated();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTagAsync(Guid id, [FromBody] UpdateTagCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        return (await _mediator.Send(command, cancellationToken)).ToOk();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTagAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new DeleteTagCommand(id), cancellationToken)).ToOk();
}
