using Asp.Versioning;
using CoreAlign.API.Middleware;
using CoreAlign.Application.Observability;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreAlign.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("client-errors")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/client-errors")]
public class ClientErrorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientErrorsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Report([FromBody] ClientErrorReportBody body, CancellationToken ct)
    {
        var correlationId = body.CorrelationId;
        if (string.IsNullOrWhiteSpace(correlationId)
            && HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var cid) && cid is string s)
        {
            correlationId = s;
        }

        var command = new ReportClientErrorCommand(
            body.Message ?? string.Empty,
            body.Severity ?? ErrorSeverity.Error,
            body.Page,
            body.Component,
            body.StackTrace,
            correlationId,
            Request.Headers.UserAgent.ToString(),
            body.ContextJson);

        await _mediator.Send(command, ct);
        return Accepted();
    }
}

public sealed record ClientErrorReportBody(
    string? Message,
    ErrorSeverity? Severity,
    string? Page,
    string? Component,
    string? StackTrace,
    string? CorrelationId,
    string? ContextJson);
