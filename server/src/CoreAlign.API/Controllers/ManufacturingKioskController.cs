using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Manufacturing.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/kiosk/manufacturing")]
public class ManufacturingKioskController : ControllerBase
{
    private readonly IMediator _mediator;
    public ManufacturingKioskController(IMediator mediator) => _mediator = mediator;

    [HttpPost("verify-pin")]
    public async Task<IActionResult> VerifyPin([FromBody] VerifyOperatorPinRequest request, CancellationToken ct)
    {
        var isValid = await _mediator.Send(new VerifyOperatorPinQuery(request.OperatorId, request.PinCode), ct);
        if (!isValid)
        {
            return Unauthorized(CoreAlign.Application.Common.ApiResponse<object>.Failure("Invalid PIN or operator is inactive.", 401));
        }
        return new { Success = true }.ToOk();
    }

    [HttpGet("work-centers/{workCenterId:guid}/active-steps")]
    public async Task<IActionResult> GetActiveSteps(Guid workCenterId, CancellationToken ct)
    {
        var steps = await _mediator.Send(new GetActiveKioskStepsQuery(workCenterId), ct);
        return steps.ToOk();
    }
}

public record VerifyOperatorPinRequest(Guid OperatorId, string PinCode);
