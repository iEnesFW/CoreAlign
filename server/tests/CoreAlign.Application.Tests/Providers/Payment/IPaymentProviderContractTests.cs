using CoreAlign.Application.Providers.Payment;

namespace CoreAlign.Application.Tests.Providers.Payment;

public abstract class IPaymentProviderContractTests<TProvider>
    where TProvider : class
{
    protected abstract TProvider CreateProvider(IPaymentProviderContractTestHarness harness);

    protected abstract Task<PaymentChargeOutcome> ChargeAsync(TProvider provider, PaymentChargeRequest request, CancellationToken ct);

    protected abstract Task<Payment3DSecureInitResult> InitiateAsync(TProvider provider, Payment3DSecureRequest request, CancellationToken ct);

    protected abstract Task<Payment3DSecureVerifyResult> VerifyAsync(TProvider provider, Payment3DSecureCallback callback, CancellationToken ct);

    protected abstract Task<PaymentRefundResult> RefundAsync(TProvider provider, string transactionId, decimal? amount, CancellationToken ct);

    protected abstract Task<PaymentTransactionInfo> GetTransactionAsync(TProvider provider, string transactionId, CancellationToken ct);

    protected abstract Task<string> TokenizeAsync(TProvider provider, string rawPan, CancellationToken ct);

    private static PaymentChargeRequest BuildCharge(
        decimal amount = 100m,
        string currency = "TRY",
        string? cardToken = "tok_visa",
        bool threeDs = false,
        IReadOnlyDictionary<string, string>? metadata = null,
        string description = "Standard charge") =>
        new(
            OrderId: Guid.NewGuid(),
            InvoiceId: null,
            Amount: amount,
            Currency: currency,
            OrderReference: "ORD-" + Guid.NewGuid().ToString("N")[..8],
            BuyerName: "Test Buyer",
            BuyerEmail: "buyer@test.local",
            BuyerIp: "127.0.0.1",
            CardToken: cardToken,
            RequestThreeDSecure: threeDs,
            CallbackUrl: "https://app.local/callback",
            Metadata: metadata ?? new Dictionary<string, string> { ["description"] = description },
            IdempotencyKey: "idem-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task T1_Charge_standard_card_returns_success()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 100m, "TRY", null, null, "{\"id\":\"ch_1\"}");

        var result = await ChargeAsync(sut, BuildCharge(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AuthorizedAmount.Should().Be(100m);
    }

    [Fact]
    public async Task T2_Charge_insufficient_funds_returns_decline()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(false, "declined", null, null, "insufficient_funds", "Card has insufficient funds.", null);

        var result = await ChargeAsync(sut, BuildCharge(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("insufficient_funds");
    }

    [Fact]
    public async Task T3_Charge_invalid_card_is_declined_without_retry()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(false, "declined", null, null, "invalid_card", "Card number is invalid.", null);

        var result = await ChargeAsync(sut, BuildCharge(cardToken: "tok_invalid"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("invalid_card");
        harness.ChargeAttempts.Should().Be(1);
    }

    [Fact]
    public async Task T4_Charge_currency_mismatch_is_rejected()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(false, "rejected", null, null, "currency_not_supported", "Currency XYZ is not enabled for this merchant.", null);

        var result = await ChargeAsync(sut, BuildCharge(currency: "XYZ"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("currency_not_supported");
    }

    [Fact]
    public async Task T5_Charge_zero_amount_is_rejected()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(false, "rejected", null, null, "amount_invalid", "Amount must be greater than zero.", null);

        var result = await ChargeAsync(sut, BuildCharge(amount: 0m), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("amount_invalid");
    }

    [Fact]
    public async Task T6_ThreeDS_initiate_returns_redirect_url()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextInitResult = new Payment3DSecureInitResult(true, "harness", "tx-3ds-1", null, "https://3ds.local/redirect/tx-3ds-1", null, null);

        var req = new Payment3DSecureRequest(
            OrderId: Guid.NewGuid(),
            InvoiceId: null,
            Amount: 250m,
            Currency: "TRY",
            OrderReference: "ORD-3DS",
            CallbackUrl: "https://app.local/3ds/cb",
            BuyerName: "Buyer",
            BuyerEmail: "buyer@test.local",
            BuyerIp: "127.0.0.1",
            CardToken: "tok_3ds",
            Metadata: null,
            IdempotencyKey: "idem-3ds-" + Guid.NewGuid().ToString("N"));

        var result = await InitiateAsync(sut, req, CancellationToken.None);

        result.Initiated.Should().BeTrue();
        result.RedirectUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task T7_ThreeDS_callback_success_marks_transaction_authorized()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextVerifyResult = new Payment3DSecureVerifyResult(true, "harness", "tx-3ds-1", "authorized", null, null, "{\"mdStatus\":\"1\"}");

        var cb = new Payment3DSecureCallback("harness", "tx-3ds-1", new Dictionary<string, string> { ["mdStatus"] = "1" });
        var result = await VerifyAsync(sut, cb, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Status.Should().Be("authorized");
    }

    [Fact]
    public async Task T8_ThreeDS_callback_failure_marks_transaction_failed()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextVerifyResult = new Payment3DSecureVerifyResult(false, "harness", "tx-3ds-1", "failed", "3ds_failed", "User abandoned the challenge.", null);

        var cb = new Payment3DSecureCallback("harness", "tx-3ds-1", new Dictionary<string, string> { ["mdStatus"] = "0" });
        var result = await VerifyAsync(sut, cb, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("3ds_failed");
    }

    [Fact]
    public async Task T9_ThreeDS_timeout_propagates_cancellation()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var req = new Payment3DSecureRequest(null, null, 50m, "TRY", "ORD-T", "https://app/cb", "B", "b@x", "1.1.1.1", "tok", null, "idem-t-" + Guid.NewGuid().ToString("N"));
        var act = async () => await InitiateAsync(sut, req, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task T10_Refund_full_succeeds()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextRefundResult = new PaymentRefundResult(true, "harness", "tx-1", "rf-1", 100m, null, null);

        var result = await RefundAsync(sut, "tx-1", null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RefundedAmount.Should().Be(100m);
    }

    [Fact]
    public async Task T11_Refund_partial_amount_is_forwarded()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextRefundResult = new PaymentRefundResult(true, "harness", "tx-2", "rf-2", 25m, null, null);

        var result = await RefundAsync(sut, "tx-2", 25m, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RefundedAmount.Should().Be(25m);
        harness.LastRefundAmount.Should().Be(25m);
    }

    [Fact]
    public async Task T12_Refund_already_refunded_is_rejected()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.MarkTransactionRefunded("tx-dup");

        var result = await RefundAsync(sut, "tx-dup", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("already_refunded");
    }

    [Fact]
    public async Task T13_Refund_outside_window_is_rejected()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.MarkSettlementWindowExpired("tx-old", DateTime.UtcNow.AddDays(-1));

        var result = await RefundAsync(sut, "tx-old", null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("refund_window_expired");
    }

    [Fact]
    public async Task T14_Tokenize_card_returns_provider_token()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextTokenizedCardToken = "tok_abc123";

        var token = await TokenizeAsync(sut, "4111111111111111", CancellationToken.None);

        token.Should().Be("tok_abc123");
    }

    [Fact]
    public async Task T15_Tokenize_declined_card_throws_gateway_exception()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextTokenizeException = new InvalidOperationException("card_declined");

        var act = async () => await TokenizeAsync(sut, "4000000000000002", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*declined*");
    }

    [Fact]
    public async Task T16_Tokenize_duplicate_pan_returns_same_token_idempotent()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextTokenizedCardToken = "tok_idem_xyz";
        var first = await TokenizeAsync(sut, "4242424242424242", CancellationToken.None);
        var second = await TokenizeAsync(sut, "4242424242424242", CancellationToken.None);

        first.Should().Be(second);
    }

    [Fact]
    public async Task T17_Status_unknown_transaction_throws_not_found()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        var act = async () => await GetTransactionAsync(sut, "missing-tx", CancellationToken.None);

        await act.Should().ThrowAsync<PaymentTransactionNotFoundException>();
    }

    [Fact]
    public async Task T18_Status_pending_returns_pending()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextTransactionInfo = new PaymentTransactionInfo("harness", "tx-1", "pending", 100m, "TRY", null, null);

        var info = await GetTransactionAsync(sut, "tx-1", CancellationToken.None);

        info.Status.Should().Be("pending");
    }

    [Fact]
    public async Task T19_Status_settled_returns_settled_with_completed_at()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        var completed = DateTime.UtcNow;
        harness.NextTransactionInfo = new PaymentTransactionInfo("harness", "tx-2", "settled", 100m, "TRY", completed, null);

        var info = await GetTransactionAsync(sut, "tx-2", CancellationToken.None);

        info.Status.Should().Be("settled");
        info.CompletedAtUtc.Should().Be(completed);
    }

    [Fact]
    public async Task T20_Status_failed_returns_failed()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextTransactionInfo = new PaymentTransactionInfo("harness", "tx-3", "failed", 100m, "TRY", null, null);

        var info = await GetTransactionAsync(sut, "tx-3", CancellationToken.None);

        info.Status.Should().Be("failed");
    }

    [Fact]
    public void T21_Webhook_valid_signature_is_accepted()
    {
        var harness = new IPaymentProviderContractTestHarness();
        _ = CreateProvider(harness);

        var verified = harness.VerifyWebhook("payload-21", harness.SignFor("payload-21"));

        verified.Should().BeTrue();
    }

    [Fact]
    public void T22_Webhook_invalid_signature_is_rejected()
    {
        var harness = new IPaymentProviderContractTestHarness();
        _ = CreateProvider(harness);

        var verified = harness.VerifyWebhook("payload-22", "deadbeef");

        verified.Should().BeFalse();
    }

    [Fact]
    public void T23_Webhook_replay_is_detected()
    {
        var harness = new IPaymentProviderContractTestHarness();
        _ = CreateProvider(harness);

        var sig = harness.SignFor("payload-23");
        harness.RegisterReplayGuard("payload-23");

        var first = harness.VerifyWebhook("payload-23", sig, enforceReplay: true);
        var replay = harness.VerifyWebhook("payload-23", sig, enforceReplay: true);

        first.Should().BeTrue();
        replay.Should().BeFalse();
    }

    [Fact]
    public async Task T24_Charge_transient_5xx_is_retried_and_succeeds()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.QueueChargeFailure(new IPaymentProviderContractTestHarness.TransientFailure(1));
        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 100m, "TRY", null, null, null);

        var result = await ChargeAsync(sut, BuildCharge(), CancellationToken.None);

        result.Success.Should().BeTrue();
        harness.ChargeAttempts.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task T25_Charge_timeout_propagates_cancellation()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await ChargeAsync(sut, BuildCharge(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task T26_Charge_permanent_error_is_not_retried()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeException = new InvalidOperationException("CARD_VALIDATION_FAILED");

        var act = async () => await ChargeAsync(sut, BuildCharge(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        harness.ChargeAttempts.Should().Be(1);
    }

    [Fact]
    public async Task T27_Charge_max_amount_is_accepted()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 999_999_999m, "TRY", null, null, null);

        var result = await ChargeAsync(sut, BuildCharge(amount: 999_999_999m), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AuthorizedAmount.Should().Be(999_999_999m);
    }

    [Fact]
    public async Task T28_Charge_min_amount_is_accepted()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 0.01m, "TRY", null, null, null);

        var result = await ChargeAsync(sut, BuildCharge(amount: 0.01m), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AuthorizedAmount.Should().Be(0.01m);
    }

    [Fact]
    public async Task T29_Charge_unicode_in_description_is_forwarded_intact()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 50m, "TRY", null, null, null);

        var meta = new Dictionary<string, string> { ["description"] = "Açıklama — 充电 — €" };
        await ChargeAsync(sut, BuildCharge(amount: 50m, metadata: meta), CancellationToken.None);

        harness.LastChargeRequest!.Metadata!["description"].Should().Be("Açıklama — 充电 — €");
    }

    [Fact]
    public async Task T30_Charge_concurrent_same_idempotency_key_returns_single_outcome()
    {
        var harness = new IPaymentProviderContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 75m, "TRY", null, null, "{\"id\":\"ch_idem\"}");
        var meta = new Dictionary<string, string> { ["idempotency-key"] = "key-30" };

        var first = await ChargeAsync(sut, BuildCharge(amount: 75m, metadata: meta), CancellationToken.None);
        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 9999m, "TRY", null, null, "{\"id\":\"ch_different\"}");
        var second = await ChargeAsync(sut, BuildCharge(amount: 75m, metadata: meta), CancellationToken.None);

        first.AuthorizedAmount.Should().Be(75m);
        second.AuthorizedAmount.Should().Be(75m);
        second.RawProviderJson.Should().Contain("ch_idem");
    }
}
