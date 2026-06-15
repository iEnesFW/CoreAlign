using System.Text.Json;
using CoreAlign.Application.Billing.Payments;

namespace CoreAlign.Infrastructure.Payments;

/// <summary>
/// Dev / demo payment gateway. <c>CreateIntentAsync</c> returns a deterministic
/// IntentId and a relative RedirectUrl that the SPA renders as an approve/cancel
/// page; <c>HandleWebhookAsync</c> accepts a tiny JSON body so the same webhook
/// pipeline used in production can be exercised end-to-end without external
/// network calls. This gateway must never be registered in non-dev environments
/// when handling real customer money.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    public const string GatewayName = "mock";

    public string Name => GatewayName;

    public Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var intentId = $"mock_{Guid.NewGuid():N}";
        var redirect = $"/dashboard/billing/mock-approve?intent={intentId}&order={request.OrderId}";
        var raw = JsonSerializer.Serialize(new
        {
            intentId,
            orderId = request.OrderId,
            orderNumber = request.OrderNumber,
            amount = request.Amount,
            currency = request.Currency,
            status = "Pending",
        });
        var metadata = new Dictionary<string, string>
        {
            ["orderId"] = request.OrderId.ToString(),
            ["orderNumber"] = request.OrderNumber,
        };
        var result = new PaymentIntentResult(intentId, redirect, PaymentIntentStatus.Pending, metadata, raw);
        return Task.FromResult(result);
    }

    public Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload)) throw new ArgumentException("Payload is required.", nameof(payload));

        var dto = JsonSerializer.Deserialize<MockWebhookPayload>(payload, SerializerOptions)
            ?? throw new ArgumentException("Payload could not be parsed.", nameof(payload));
        if (string.IsNullOrWhiteSpace(dto.IntentId)) throw new ArgumentException("intentId is required in mock webhook payload.", nameof(payload));

        var status = (dto.Action ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "approve" => PaymentIntentStatus.Succeeded,
            "cancel" => PaymentIntentStatus.Cancelled,
            "fail" => PaymentIntentStatus.Failed,
            _ => throw new ArgumentException($"Unknown action '{dto.Action}'.", nameof(payload)),
        };

        var result = new WebhookProcessingResult(
            dto.IntentId,
            status,
            string.IsNullOrWhiteSpace(dto.Reference) ? null : dto.Reference,
            status == PaymentIntentStatus.Failed ? dto.Reason : null,
            payload);
        return Task.FromResult(result);
    }

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = new CaptureResult(true, $"mock_capture_{Guid.NewGuid():N}", null, null);
        return Task.FromResult(result);
    }

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = new RefundResult(true, $"mock_refund_{Guid.NewGuid():N}", null, null);
        return Task.FromResult(result);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record MockWebhookPayload(string? IntentId, string? Action, string? Reference, string? Reason);
}
