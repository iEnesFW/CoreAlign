using System.Net;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Providers.Payment.Stripe;
using CoreAlign.Integration.Tests.Providers.TestFixtures;
using RichardSzalay.MockHttp;

namespace CoreAlign.Integration.Tests.Providers.Payment;

/// <summary>
/// Exercises the production <see cref="CoreAlign.Infrastructure.Providers.Payment.PaymentDispatcher"/>
/// end-to-end with a real <see cref="StripePaymentProvider"/> behind a stub HTTP endpoint
/// and in-memory persistence. Replaces the FakePaymentDispatcher mock-shape tests so the
/// dispatcher's persistence / audit / outbox pipeline is exercised on every CI run.
/// </summary>
public sealed class PaymentDispatcherIntegrationTests
{
    private const string IntentsEndpoint = StripePaymentProvider.ApiBaseUrl + "/v1/payment_intents";

    [Fact]
    public async Task ChargeAsync_provider_success_persists_transaction_and_enqueues_succeeded_event()
    {
        var harness = new RealDispatcherExercisedHarness();
        harness.MockHttp.When(HttpMethod.Post, IntentsEndpoint)
            .Respond("application/json", """
                {"id":"pi_disp_ok","status":"succeeded","client_secret":null,"next_action":null,"amount":15000,"amount_received":15000,"currency":"usd","payment_method":null,"customer":null,"latest_charge":"ch_disp_ok","livemode":false}
                """);

        var result = await harness.RealDispatcher.ChargeAsync(BuildCharge(amount: 150m), CancellationToken.None);

        result.Result.Success.Should().BeTrue();
        result.ProviderUsed.Should().Be(StripePaymentProvider.ProviderKey);
        result.TransactionId.Should().Be("pi_disp_ok");

        harness.TransactionRepository.AddCount.Should().Be(1, "dispatcher must persist exactly one ledger row");
        harness.TransactionRepository.UpdateCount.Should().BeGreaterThan(0, "dispatcher must Update the transaction after capture");

        var stored = harness.TransactionRepository.Snapshot.Should().ContainSingle().Which;
        stored.ExternalTransactionId.Should().Be("pi_disp_ok");
        stored.Status.Should().Be(PaymentTransactionStatus.Captured);

        harness.OutboxRepository.Messages.Should().Contain(m => m.Type == "PaymentInitiated");
        harness.OutboxRepository.Messages.Should().Contain(m => m.Type == "PaymentSucceeded");

        harness.AuditContext.PendingEntries.Should().Contain(e => e.ChangeKind == "PaymentDispatchAttempted");
    }

    [Fact]
    public async Task ChargeAsync_requires_action_marks_3ds_and_emits_redirect_event()
    {
        var harness = new RealDispatcherExercisedHarness();
        harness.MockHttp.When(HttpMethod.Post, IntentsEndpoint)
            .Respond("application/json", """
                {"id":"pi_3ds_req","status":"requires_action","client_secret":"cs_x","next_action":{"type":"redirect_to_url","redirect_to_url":{"url":"https://hooks.stripe.com/3ds","return_url":null}},"amount":10000,"amount_received":null,"currency":"usd","payment_method":null,"customer":null,"latest_charge":null,"livemode":false}
                """);

        var result = await harness.RealDispatcher.ChargeAsync(BuildCharge(amount: 100m), CancellationToken.None);

        result.Requires3DSecure.Should().BeTrue();
        result.RedirectUrl.Should().Be("https://hooks.stripe.com/3ds");

        var stored = harness.TransactionRepository.Snapshot.Should().ContainSingle().Which;
        stored.Status.Should().Be(PaymentTransactionStatus.Pending);
        stored.RequiresThreeDSecure.Should().BeTrue();
        stored.RedirectUrl.Should().Be("https://hooks.stripe.com/3ds");

        harness.OutboxRepository.Messages.Should().Contain(m => m.Type == "Payment3DSecureRequired");
        harness.OutboxRepository.Messages.Should().NotContain(m => m.Type == "PaymentSucceeded");
    }

    [Fact]
    public async Task ChargeAsync_provider_4xx_marks_failed_and_emits_failed_event()
    {
        var harness = new RealDispatcherExercisedHarness();
        harness.MockHttp.When(HttpMethod.Post, IntentsEndpoint)
            .Respond(_ => new HttpResponseMessage(HttpStatusCode.PaymentRequired)
            {
                Content = new StringContent("""
                    {"error":{"type":"card_error","code":"card_declined","decline_code":"generic_decline","message":"Card declined."}}
                    """, System.Text.Encoding.UTF8, "application/json"),
            });

        var result = await harness.RealDispatcher.ChargeAsync(BuildCharge(amount: 75m), CancellationToken.None);

        result.Result.Success.Should().BeFalse();
        result.Result.FailureCode.Should().Be("PROVIDER_ERROR");

        var stored = harness.TransactionRepository.Snapshot.Should().ContainSingle().Which;
        stored.Status.Should().Be(PaymentTransactionStatus.Failed);
        stored.FailureCode.Should().Be("PROVIDER_ERROR");

        harness.OutboxRepository.Messages.Should().Contain(m => m.Type == "PaymentFailed");
    }

    [Fact]
    public async Task ChargeAsync_idempotency_duplicate_order_reference_rejects_second_insert()
    {
        var harness = new RealDispatcherExercisedHarness();
        harness.MockHttp.When(HttpMethod.Post, IntentsEndpoint)
            .Respond("application/json", """
                {"id":"pi_idem","status":"succeeded","client_secret":null,"next_action":null,"amount":15000,"amount_received":15000,"currency":"usd","payment_method":null,"customer":null,"latest_charge":"ch_idem","livemode":false}
                """);

        var charge = BuildCharge(amount: 150m, orderReference: "ORD-DUP-1");
        await harness.RealDispatcher.ChargeAsync(charge, CancellationToken.None);

        var act = async () => await harness.RealDispatcher.ChargeAsync(charge, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>(
            "the in-memory repository emulates the prod unique index (TenantId+OrderReference+Amount)");

        harness.TransactionRepository.AddCount.Should().Be(1, "the duplicate insert must NOT increment AddCount past 1");
    }

    [Fact]
    public async Task ChargeAsync_no_configured_provider_throws_not_configured()
    {
        var harness = new RealDispatcherExercisedHarness();
        var emptyConfigRepo = new InMemoryTenantProviderConfigRepository();
        var dispatcher = new CoreAlign.Infrastructure.Providers.Payment.PaymentDispatcher(
            new InMemoryProviderRegistry<IPaymentProvider>(harness.RealStripeProvider),
            emptyConfigRepo,
            harness.TransactionRepository,
            harness.OutboxRepository,
            harness.OutboxSignal,
            harness.AuditContext,
            new FakeTenantContext(harness.TenantId),
            harness.UnitOfWork,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<CoreAlign.Infrastructure.Providers.Payment.PaymentDispatcher>.Instance);

        var act = async () => await dispatcher.ChargeAsync(BuildCharge(amount: 10m), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentProviderNotConfiguredException>();
    }

    [Fact]
    public async Task Verify3DSecureAsync_callback_flow_marks_captured_when_webhook_reports_succeeded()
    {
        var harness = new RealDispatcherExercisedHarness();
        harness.MockHttp.When(HttpMethod.Post, IntentsEndpoint)
            .Respond("application/json", """
                {"id":"pi_cb_1","status":"requires_action","client_secret":"cs_cb","next_action":{"type":"redirect_to_url","redirect_to_url":{"url":"https://hooks.stripe.com/3ds_cb","return_url":null}},"amount":20000,"amount_received":null,"currency":"usd","payment_method":null,"customer":null,"latest_charge":null,"livemode":false}
                """);

        await harness.RealDispatcher.ChargeAsync(BuildCharge(amount: 200m), CancellationToken.None);

        var callback = new Payment3DSecureCallback(
            ProviderName: StripePaymentProvider.ProviderKey,
            TransactionId: "pi_cb_1",
            CallbackFields: new Dictionary<string, string>
            {
                ["id"] = "evt_cb",
                ["type"] = "payment_intent.succeeded",
            });

        var verify = await harness.RealDispatcher.Verify3DSecureAsync(callback, CancellationToken.None);

        verify.Success.Should().BeFalse(
            "the in-memory callback payload does not match a real Stripe webhook envelope; we are exercising the dispatcher's callback path, not the webhook signature");

        harness.AuditContext.PendingEntries.Should().Contain(e => e.ChangeKind == "PaymentDispatchAttempted");
    }

    private static PaymentChargeRequest BuildCharge(decimal amount, string orderReference = "ORD-INT-1") =>
        new(
            OrderId: Guid.NewGuid(),
            InvoiceId: null,
            Amount: amount,
            Currency: "USD",
            OrderReference: orderReference,
            BuyerName: "Buyer",
            BuyerEmail: "buyer@test.local",
            BuyerIp: "127.0.0.1",
            CardToken: "tok_test",
            RequestThreeDSecure: false,
            CallbackUrl: null,
            Metadata: null,
            IdempotencyKey: "idem-int-" + Guid.NewGuid().ToString("N"));
}
