using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Payment;
using BillingIntentRequest = CoreAlign.Application.Billing.Payments.PaymentIntentRequest;
using LinkIntentRequest = CoreAlign.Application.Providers.Payment.PaymentIntentRequest;

namespace CoreAlign.Application.Tests.Providers.Payment;

public sealed class HarnessBackedPaymentProvider : IPaymentProvider
{
    private readonly IPaymentProviderContractTestHarness _harness;
    private readonly int _maxRetriesOnTransient;

    public HarnessBackedPaymentProvider(string name, IPaymentProviderContractTestHarness harness, int maxRetriesOnTransient = 3)
    {
        Name = name;
        _harness = harness;
        _maxRetriesOnTransient = maxRetriesOnTransient;
    }

    public string Name { get; }

    public string DisplayName => Name;

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.Refund | ProviderCapability.Webhook,
        new Dictionary<string, string> { ["mode"] = "contract-test" });

    public Task<IReadOnlyList<PaymentMethodDescriptor>> ListMethodsAsync(Guid tenantId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PaymentMethodDescriptor>>(new[]
        {
            new PaymentMethodDescriptor(
                PaymentMethodKind.CardOnFile,
                "Card",
                1m,
                999_999_999m,
                new[] { "TRY", "USD", "EUR" }),
        });

    public Task<PaymentLinkResult> CreateLinkAsync(LinkIntentRequest req, PaymentLinkOptions opts, CancellationToken ct) =>
        Task.FromResult(new PaymentLinkResult(
            $"https://{Name}.local/pay/{req.OrderReference}",
            DateTime.UtcNow.AddMinutes(opts.ExpiryMinutes),
            Guid.NewGuid().ToString("N")));

    public Task<PaymentIntentResult> CreateIntentAsync(BillingIntentRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentIntentResult(
            Guid.NewGuid().ToString(),
            $"https://{Name}.local/intent",
            PaymentIntentStatus.Pending,
            new Dictionary<string, string>(),
            null));

    public Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var signature = headers.TryGetValue("x-signature", out var s) ? s : string.Empty;
        if (!_harness.VerifyWebhook(payload, signature))
        {
            throw new PaymentWebhookSignatureException("Invalid signature.");
        }
        return Task.FromResult(new WebhookProcessingResult("intent-1", PaymentIntentStatus.Succeeded, "ref-1", null, payload));
    }

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new CaptureResult(true, request.IntentId, null, null));

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new RefundResult(true, Guid.NewGuid().ToString(), null, null));

    public async Task<PaymentChargeOutcome> ChargeAsync(PaymentChargeRequest request, CancellationToken ct)
    {
        for (var attempt = 0; attempt <= _maxRetriesOnTransient; attempt++)
        {
            try
            {
                return await _harness.RecordChargeAsync(request, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < _maxRetriesOnTransient)
            {
            }
        }
        throw new HttpRequestException("Retry budget exhausted.");
    }

    public Task<Payment3DSecureInitResult> InitiateAsync(Payment3DSecureRequest request, CancellationToken ct) =>
        _harness.RecordInitiateAsync(request, ct);

    public Task<Payment3DSecureVerifyResult> VerifyAsync(Payment3DSecureCallback callback, CancellationToken ct) =>
        _harness.RecordVerifyAsync(callback, ct);

    public Task<PaymentRefundResult> RefundProviderAsync(string transactionId, decimal? amount, CancellationToken ct) =>
        _harness.RecordRefundAsync(transactionId, amount, ct);

    public async Task<PaymentTransactionInfo> GetTransactionAsync(string transactionId, CancellationToken ct)
    {
        for (var attempt = 0; attempt <= _maxRetriesOnTransient; attempt++)
        {
            try
            {
                return await _harness.RecordGetTransactionAsync(transactionId, ct).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < _maxRetriesOnTransient)
            {
            }
        }
        throw new HttpRequestException("Retry budget exhausted.");
    }

    public Task<string> TokenizeAsync(string rawPan, CancellationToken ct) =>
        _harness.RecordTokenizeAsync(rawPan, ct);
}
