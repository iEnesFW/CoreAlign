using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Common;
using CoreAlign.Application.Quotes.Commands;
using CoreAlign.Application.Quotes.Queries;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize(Policy = Authorization.PersonaPolicies.Tenant)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuotesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] QuoteStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetQuotesQuery(page, pageSize, search, customerId, status), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetQuoteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuoteByIdQuery(id), cancellationToken);
        return result.ToOk();
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetQuotePdfAsync(Guid id, CancellationToken cancellationToken)
    {
        var pdf = await _mediator.Send(new GetQuotePdfQuery(id), cancellationToken);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuoteAsync([FromBody] CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToCreated();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateQuoteAsync(Guid id, [FromBody] UpdateQuoteCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<object>.Failure("Route id does not match command id.", 400));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToOk();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteQuoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteQuoteCommand(id), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> SendQuoteAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new SendQuoteCommand(id), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> AcceptQuoteAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new AcceptQuoteCommand(id), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectQuoteAsync(Guid id, [FromBody] RejectQuoteRequest? body, CancellationToken cancellationToken)
        => (await _mediator.Send(new RejectQuoteCommand(id, body?.Reason), cancellationToken)).ToOk();

    [HttpPost("{id:guid}/convert-to-order")]
    public async Task<IActionResult> ConvertToOrderAsync(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new ConvertQuoteToOrderCommand(id), cancellationToken)).ToCreated();
}

public record RejectQuoteRequest(string? Reason);
