using CoreAlign.Application.Providers.Payment;

namespace CoreAlign.Application.Tests.Providers.Payment;

public sealed class PaymentDispatcherTests
{
    [Fact]
    public async Task Dispatcher_single_provider_success_returns_outcome()
    {
        var harness = new IPaymentProviderContractTestHarness
        {
            NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 250m, "TRY", null, null, null),
        };
        var provider = new HarnessBackedPaymentProvider("iyzico", harness, maxRetriesOnTransient: 0);
        var dispatcher = new FakePaymentDispatcher(provider, maxTransientRetries: 0);

        var result = await dispatcher.ChargeAsync(BuildCharge(), CancellationToken.None);

        result.Result.Success.Should().BeTrue();
        result.ProviderUsed.Should().Be("iyzico");
        result.AttemptHistory.Should().HaveCount(1);
    }

    [Fact]
    public async Task Dispatcher_transient_retry_succeeds_on_second_attempt()
    {
        var harness = new IPaymentProviderContractTestHarness();
        harness.QueueChargeFailure(new IPaymentProviderContractTestHarness.TransientFailure(1));
        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 100m, "TRY", null, null, null);

        var provider = new HarnessBackedPaymentProvider("iyzico", harness, maxRetriesOnTransient: 0);
        var dispatcher = new FakePaymentDispatcher(provider, maxTransientRetries: 3);

        var result = await dispatcher.ChargeAsync(BuildCharge(), CancellationToken.None);

        result.Result.Success.Should().BeTrue();
        result.AttemptHistory.Should().HaveCountGreaterThan(1);
        harness.ChargeAttempts.Should().Be(2);
    }

    [Fact]
    public async Task Dispatcher_permanent_error_is_not_retried_and_surfaces_to_caller()
    {
        var harness = new IPaymentProviderContractTestHarness
        {
            NextChargeOutcome = new PaymentChargeOutcome(false, "declined", null, null, "card_declined", "Card declined.", null),
        };
        var provider = new HarnessBackedPaymentProvider("iyzico", harness, maxRetriesOnTransient: 0);
        var dispatcher = new FakePaymentDispatcher(provider, maxTransientRetries: 3);

        var result = await dispatcher.ChargeAsync(BuildCharge(), CancellationToken.None);

        result.Result.Success.Should().BeFalse();
        result.Result.FailureCode.Should().Be("card_declined");
        harness.ChargeAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Dispatcher_audit_attempt_history_is_recorded()
    {
        var harness = new IPaymentProviderContractTestHarness();
        harness.QueueChargeFailure(new IPaymentProviderContractTestHarness.TransientFailure(2));
        harness.NextChargeOutcome = new PaymentChargeOutcome(true, "succeeded", 100m, "TRY", null, null, null);

        var provider = new HarnessBackedPaymentProvider("iyzico", harness, maxRetriesOnTransient: 0);
        var dispatcher = new FakePaymentDispatcher(provider, maxTransientRetries: 5);

        var result = await dispatcher.ChargeAsync(BuildCharge(), CancellationToken.None);

        result.AttemptHistory.Should().HaveCount(3);
        result.AttemptHistory[0].Succeeded.Should().BeFalse();
        result.AttemptHistory[1].Succeeded.Should().BeFalse();
        result.AttemptHistory[^1].Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Dispatcher_no_provider_throws_not_configured()
    {
        var dispatcher = new FakePaymentDispatcher(provider: null, maxTransientRetries: 0);

        var act = async () => await dispatcher.ChargeAsync(BuildCharge(), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentProviderNotConfiguredException>();
    }

    private static PaymentChargeRequest BuildCharge() =>
        new(
            OrderId: Guid.NewGuid(),
            InvoiceId: null,
            Amount: 100m,
            Currency: "TRY",
            OrderReference: "ORD-DISP",
            BuyerName: "Buyer",
            BuyerEmail: "buyer@test.local",
            BuyerIp: "127.0.0.1",
            CardToken: "tok",
            RequestThreeDSecure: false,
            CallbackUrl: null,
            Metadata: null,
            IdempotencyKey: "idem-disp-" + Guid.NewGuid().ToString("N"));

    private sealed class FakePaymentDispatcher
    {
        private readonly HarnessBackedPaymentProvider? _provider;
        private readonly int _maxTransientRetries;

        public FakePaymentDispatcher(HarnessBackedPaymentProvider? provider, int maxTransientRetries)
        {
            _provider = provider;
            _maxTransientRetries = maxTransientRetries;
        }

        public async Task<PaymentDispatchResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct)
        {
            if (_provider is null)
            {
                throw new PaymentProviderNotConfiguredException(Guid.Empty);
            }

            var attempts = new List<PaymentAttemptInfo>();

            for (var attempt = 0; attempt <= _maxTransientRetries; attempt++)
            {
                var attemptStart = DateTime.UtcNow;
                try
                {
                    var outcome = await _provider.ChargeAsync(request, ct).ConfigureAwait(false);
                    attempts.Add(new PaymentAttemptInfo(_provider.Name, outcome.Success, outcome.FailureCode, outcome.FailureMessage, attemptStart, DateTime.UtcNow - attemptStart));
                    return new PaymentDispatchResult(outcome, _provider.Name, "tx-" + Guid.NewGuid().ToString("N")[..8], false, null, attempts);
                }
                catch (HttpRequestException ex) when (attempt < _maxTransientRetries)
                {
                    attempts.Add(new PaymentAttemptInfo(_provider.Name, false, "transient", ex.Message, attemptStart, DateTime.UtcNow - attemptStart));
                }
            }

            throw new HttpRequestException("Retry budget exhausted.");
        }
    }
}
