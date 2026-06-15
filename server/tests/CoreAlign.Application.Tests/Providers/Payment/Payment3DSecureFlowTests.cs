using CoreAlign.Application.Providers.Payment;

namespace CoreAlign.Application.Tests.Providers.Payment;

public sealed class Payment3DSecureFlowTests
{
    [Fact]
    public async Task Init_returns_redirect_url_for_user_challenge()
    {
        var harness = new IPaymentProviderContractTestHarness
        {
            NextInitResult = new Payment3DSecureInitResult(true, "iyzico", "tx-3ds-1", null, "https://iyzico.local/3ds/tx-3ds-1", null, null),
        };
        var provider = new HarnessBackedPaymentProvider("iyzico", harness, maxRetriesOnTransient: 0);

        var req = new Payment3DSecureRequest(Guid.NewGuid(), null, 150m, "TRY", "ORD-3DS", "https://app.local/cb", "Buyer", "buyer@test.local", "127.0.0.1", "tok_3ds", null, "idem-flow-" + Guid.NewGuid().ToString("N"));
        var result = await provider.InitiateAsync(req, CancellationToken.None);

        result.Initiated.Should().BeTrue();
        result.RedirectUrl.Should().StartWith("https://");
        harness.LastInitRequest!.OrderReference.Should().Be("ORD-3DS");
    }

    [Fact]
    public async Task Callback_success_captures_transaction()
    {
        var harness = new IPaymentProviderContractTestHarness
        {
            NextVerifyResult = new Payment3DSecureVerifyResult(true, "iyzico", "tx-3ds-1", "authorized", null, null, "{\"mdStatus\":\"1\"}"),
        };
        var provider = new HarnessBackedPaymentProvider("iyzico", harness, maxRetriesOnTransient: 0);

        var cb = new Payment3DSecureCallback("iyzico", "tx-3ds-1", new Dictionary<string, string> { ["mdStatus"] = "1" });
        var result = await provider.VerifyAsync(cb, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Status.Should().Be("authorized");
        harness.LastVerifyCallback!.TransactionId.Should().Be("tx-3ds-1");
    }

    [Fact]
    public async Task Callback_failure_marks_transaction_failed()
    {
        var harness = new IPaymentProviderContractTestHarness
        {
            NextVerifyResult = new Payment3DSecureVerifyResult(false, "iyzico", "tx-3ds-2", "failed", "3ds_user_cancelled", "User abandoned challenge.", null),
        };
        var provider = new HarnessBackedPaymentProvider("iyzico", harness, maxRetriesOnTransient: 0);

        var cb = new Payment3DSecureCallback("iyzico", "tx-3ds-2", new Dictionary<string, string> { ["mdStatus"] = "0" });
        var result = await provider.VerifyAsync(cb, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureCode.Should().Be("3ds_user_cancelled");
    }
}
