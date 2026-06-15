using CoreAlign.Application.Providers.Payment;

namespace CoreAlign.Application.Tests.Providers.Payment;

public sealed class StripeContractTests : IPaymentProviderContractTests<HarnessBackedPaymentProvider>
{
    protected override HarnessBackedPaymentProvider CreateProvider(IPaymentProviderContractTestHarness harness) =>
        new("stripe", harness);

    protected override Task<PaymentChargeOutcome> ChargeAsync(HarnessBackedPaymentProvider provider, PaymentChargeRequest request, CancellationToken ct) =>
        provider.ChargeAsync(request, ct);

    protected override Task<Payment3DSecureInitResult> InitiateAsync(HarnessBackedPaymentProvider provider, Payment3DSecureRequest request, CancellationToken ct) =>
        provider.InitiateAsync(request, ct);

    protected override Task<Payment3DSecureVerifyResult> VerifyAsync(HarnessBackedPaymentProvider provider, Payment3DSecureCallback callback, CancellationToken ct) =>
        provider.VerifyAsync(callback, ct);

    protected override Task<PaymentRefundResult> RefundAsync(HarnessBackedPaymentProvider provider, string transactionId, decimal? amount, CancellationToken ct) =>
        provider.RefundProviderAsync(transactionId, amount, ct);

    protected override Task<PaymentTransactionInfo> GetTransactionAsync(HarnessBackedPaymentProvider provider, string transactionId, CancellationToken ct) =>
        provider.GetTransactionAsync(transactionId, ct);

    protected override Task<string> TokenizeAsync(HarnessBackedPaymentProvider provider, string rawPan, CancellationToken ct) =>
        provider.TokenizeAsync(rawPan, ct);
}
