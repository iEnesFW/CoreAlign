using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Infrastructure.Payments;
using PaymentIntentRequest = CoreAlign.Application.Providers.Payment.PaymentIntentRequest;
using BillingPaymentIntentRequest = CoreAlign.Application.Billing.Payments.PaymentIntentRequest;

namespace CoreAlign.Infrastructure.Providers.Payment;

public sealed class MockPaymentProvider : IPaymentProvider
{
    private static readonly Guid Namespace = new("4f6a4b3a-1d12-4f5d-9b3e-8b9c1a2d3e4f");

    private readonly MockPaymentGateway _gateway = new();

    public string Name => "mock";

    public string DisplayName => "Mock Payment Provider";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.WebhookCallback | ProviderCapability.RealTimeStatus,
        new Dictionary<string, string> { ["env"] = "dev" });

    public Task<IReadOnlyList<PaymentMethodDescriptor>> ListMethodsAsync(Guid tenantId, CancellationToken ct)
    {
        IReadOnlyList<PaymentMethodDescriptor> list = new[]
        {
            new PaymentMethodDescriptor(
                PaymentMethodKind.ThreeDS,
                "Mock 3DS Card",
                MinAmount: 1m,
                MaxAmount: 1_000_000m,
                SupportedCurrencies: new[] { "TRY", "USD", "EUR" }),
        };
        return Task.FromResult(list);
    }

    public Task<PaymentLinkResult> CreateLinkAsync(PaymentIntentRequest req, PaymentLinkOptions opts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(opts);

        var seed = $"{req.OrderReference}|{req.Amount}|{req.Currency}|{req.BuyerEmail}";
        var providerRefId = DeterministicGuid(seed).ToString("N");
        var expires = DateTime.UtcNow.AddMinutes(opts.ExpiryMinutes <= 0 ? 30 : opts.ExpiryMinutes);
        var result = new PaymentLinkResult(
            LinkUrl: $"https://mock.payment/link/{providerRefId}",
            ExpiresAtUtc: expires,
            ProviderRefId: providerRefId);
        return Task.FromResult(result);
    }

    public Task<PaymentIntentResult> CreateIntentAsync(BillingPaymentIntentRequest request, CancellationToken cancellationToken)
        => _gateway.CreateIntentAsync(request, cancellationToken);

    public Task<WebhookProcessingResult> HandleWebhookAsync(string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
        => _gateway.HandleWebhookAsync(payload, headers, cancellationToken);

    public Task<CaptureResult> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken)
        => _gateway.CaptureAsync(request, cancellationToken);

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken cancellationToken)
        => _gateway.RefundAsync(request, cancellationToken);

    private static Guid DeterministicGuid(string seed)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Namespace + "|" + seed));
        var guid = new byte[16];
        Array.Copy(bytes, guid, 16);
        return new Guid(guid);
    }
}
