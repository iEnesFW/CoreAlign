using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Documents.Forwarding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.CustomerPortal;

[ApiController]
[Authorize(Policy = CustomerPortalPolicies.SelfService)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal/documents")]
public class MyDocumentForwardController : ControllerBase
{
    private readonly IMediator _mediator;

    public MyDocumentForwardController(IMediator mediator) => _mediator = mediator;

    [HttpPost("forward")]
    public async Task<IActionResult> Forward([FromBody] ForwardDocumentRequestBody body, CancellationToken ct)
    {
        var command = new ForwardCustomerDocumentCommand(
            body.DocumentType,
            body.DocumentId,
            body.RecipientEmail,
            IdempotencyKeyReader.Resolve(Request));
        return (await _mediator.Send(command, ct)).ToOk();
    }
}

public sealed record ForwardDocumentRequestBody(
    ForwardableDocumentType DocumentType,
    Guid DocumentId,
    string RecipientEmail);

internal static class IdempotencyKeyReader
{
    public static Guid Resolve(HttpRequest request) =>
        request.Headers.TryGetValue("Idempotency-Key", out var value) && Guid.TryParse(value, out var key)
            ? key
            : Guid.NewGuid();
}
