using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/billing")]
public class BillingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BillingController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsTenantAdmin() => User.IsInRole("TenantAdmin");

    [HttpGet("orders")]
    public async Task<IActionResult> ListOrders(
        [FromQuery] SubscriptionOrderStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
        => (await _mediator.Send(new ListSubscriptionOrdersQuery(status, page, pageSize), cancellationToken)).ToOk();

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
        => (await _mediator.Send(new GetSubscriptionOrderByIdQuery(id), cancellationToken)).ToOk();

    [HttpGet("gateways")]
    public async Task<IActionResult> ListGateways(CancellationToken cancellationToken)
        => (await _mediator.Send(new ListPaymentGatewaysQuery(), cancellationToken)).ToOk();

    [HttpPost("orders")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateSubscriptionOrderCommand command, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var enriched = command with
        {
            CurrentUserId = CurrentUserId(),
            BuyerIpAddress = string.IsNullOrWhiteSpace(command.BuyerIpAddress) ? ip : command.BuyerIpAddress,
        };
        return (await _mediator.Send(enriched, cancellationToken)).ToCreated();
    }

    public record CancelOrderRequest(string? Reason);

    [HttpPost("orders/{id:guid}/cancel")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest? body, CancellationToken cancellationToken)
        => (await _mediator.Send(new CancelSubscriptionOrderCommand(id, body?.Reason, CurrentUserId(), IsTenantAdmin()), cancellationToken)).ToOk();

    public record MockApproveRequest(string Action);

    [HttpPost("orders/{id:guid}/mock-approve")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<IActionResult> MockApprove(Guid id, [FromBody] MockApproveRequest body, CancellationToken cancellationToken)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Action))
        {
            return BadRequest(ApiResponse<object>.Failure("Action is required.", 400));
        }
        var result = await _mediator.Send(new ApplyMockPaymentApprovalCommand(id, body.Action, CurrentUserId()), cancellationToken);
        return result.ToOk();
    }

    [HttpPost("webhooks/{gatewayName}")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(string gatewayName, CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        if (Request.ContentLength is null || Request.ContentLength > 65536)
        {
            return BadRequest(ApiResponse<object>.Failure("Webhook payload too large.", 400));
        }

        string payload;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true, detectEncodingFromByteOrderMarks: false))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }
        Request.Body.Position = 0;

        var headers = Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        try
        {
            var result = await _mediator.Send(
                new ProcessPaymentWebhookCommand(gatewayName, payload, headers),
                cancellationToken);

            if (!result.Accepted)
            {
                return BadRequest(ApiResponse<object>.Failure(result.Message ?? "Webhook rejected.", 400));
            }
            return result.ToOk();
        }
        catch (PaymentWebhookSignatureException ex)
        {
            return Unauthorized(ApiResponse<object>.Failure(ex.Message, 401));
        }
    }
}
