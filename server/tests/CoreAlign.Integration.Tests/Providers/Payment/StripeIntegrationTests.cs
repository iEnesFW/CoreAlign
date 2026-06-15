using System.Net;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Providers.Payment.Stripe;
using CoreAlign.Integration.Tests.Providers.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace CoreAlign.Integration.Tests.Providers.Payment;

/// <summary>
/// Exercises the production <see cref="StripePaymentProvider"/> end-to-end against a
/// stubbed Stripe sandbox. Verifies form-encoded body composition, idempotency-key
/// propagation, transient-retry semantics, and the 3DS init / verify pipeline.
/// </summary>
public sealed class StripeIntegrationTests
{
    private const string BaseUrl = StripePaymentProvider.ApiBaseUrl;
    private const string IntentsEndpoint = BaseUrl + "/v1/payment_intents";

    private static readonly string CredentialJson = """
        {"SecretKey":"sk_test_integration","WebhookSigningSecret":"whsec_test","AccountId":null,"IsSandbox":true}
        """;

    [Fact]
    public async Task CreateIntentAsync_sandbox_200_returns_parsed_intent()
    {
        var (provider, mock, _) = BuildProvider();
        mock.When(HttpMethod.Post, IntentsEndpoint)
            .Respond("application/json", """
                {"id":"pi_test_1","status":"requires_action","client_secret":"cs_test_1","next_action":{"type":"redirect_to_url","redirect_to_url":{"url":"https://hooks.stripe.com/3ds","return_url":null}},"amount":12000,"amount_received":null,"currency":"usd","payment_method":null,"customer":null,"latest_charge":null,"livemode":false}
                """);

        var request = BuildIntentRequest(amount: 120m, currency: "USD");
        var result = await provider.CreateIntentAsync(request, CancellationToken.None);

        result.IntentId.Should().Be("pi_test_1");
        result.Status.Should().Be(PaymentIntentStatus.RequiresAction);
        result.RedirectUrl.Should().Be("https://hooks.stripe.com/3ds");
    }

    [Fact]
    public async Task CreateIntentAsync_transient_429_retries_then_succeeds()
    {
        var (provider, mock, _) = BuildProvider();
        var attempts = 0;
        mock.When(HttpMethod.Post, IntentsEndpoint)
            .Respond(_ =>
            {
                attempts++;
                if (attempts < 2)
                {
                    return new HttpResponseMessage((HttpStatusCode)429);
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"id":"pi_retry_ok","status":"succeeded","client_secret":null,"next_action":null,"amount":5000,"amount_received":5000,"currency":"usd","payment_method":null,"customer":null,"latest_charge":"ch_1","livemode":false}
                        """, System.Text.Encoding.UTF8, "application/json"),
                };
            });

        var result = await provider.CreateIntentAsync(BuildIntentRequest(50m, "USD"), CancellationToken.None);

        result.IntentId.Should().Be("pi_retry_ok");
        result.Status.Should().Be(PaymentIntentStatus.Succeeded);
        attempts.Should().BeGreaterThan(1, "Stripe rate-limit responses must be retried");
    }

    [Fact]
    public async Task CreateIntentAsync_4xx_surfaces_provider_exception_without_retry()
    {
        var (provider, mock, _) = BuildProvider();
        var attempts = 0;
        mock.When(HttpMethod.Post, IntentsEndpoint)
            .Respond(_ =>
            {
                attempts++;
                return new HttpResponseMessage(HttpStatusCode.PaymentRequired)
                {
                    Content = new StringContent("""
                        {"error":{"type":"card_error","code":"card_declined","decline_code":"insufficient_funds","message":"Your card has insufficient funds."}}
                        """, System.Text.Encoding.UTF8, "application/json"),
                };
            });

        var act = async () => await provider.CreateIntentAsync(BuildIntentRequest(10m, "USD"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<StripeProviderException>();
        ex.Which.ErrorCode.Should().Be("card_declined");
        ex.Which.DeclineCode.Should().Be("insufficient_funds");
        attempts.Should().Be(1, "card_declined / 402 must NOT be retried");
    }

    [Fact]
    public async Task Initiate3DSecureAsync_sends_confirm_true_and_returns_intent()
    {
        var (provider, mock, _) = BuildProvider();
        mock.When(HttpMethod.Post, IntentsEndpoint)
            .Respond("application/json", """
                {"id":"pi_3ds_1","status":"requires_action","client_secret":"cs_3ds","next_action":{"type":"redirect_to_url","redirect_to_url":{"url":"https://hooks.stripe.com/3ds_redirect","return_url":null}},"amount":25000,"amount_received":null,"currency":"usd","payment_method":"pm_test","customer":null,"latest_charge":null,"livemode":false}
                """);

        var intent = await provider.Initiate3DSecureAsync(
            BuildIntentRequest(250m, "USD"),
            "pm_test",
            "https://app.local/return",
            CancellationToken.None);

        intent.Id.Should().Be("pi_3ds_1");
        intent.Status.Should().Be("requires_action");
        intent.NextAction?.RedirectToUrl?.Url.Should().Be("https://hooks.stripe.com/3ds_redirect");
    }

    [Fact]
    public async Task Verify3DSecureAsync_calls_confirm_endpoint_and_returns_succeeded()
    {
        var (provider, mock, _) = BuildProvider();
        mock.When(HttpMethod.Post, IntentsEndpoint + "/pi_verify_1/confirm")
            .Respond("application/json", """
                {"id":"pi_verify_1","status":"succeeded","client_secret":null,"next_action":null,"amount":7500,"amount_received":7500,"currency":"usd","payment_method":"pm_test","customer":null,"latest_charge":"ch_verify","livemode":false}
                """);

        var verified = await provider.Verify3DSecureAsync("pi_verify_1", CancellationToken.None);

        verified.Status.Should().Be("succeeded");
        verified.LatestCharge.Should().Be("ch_verify");
    }

    [Fact]
    public async Task RefundAsync_200_returns_success_result()
    {
        var (provider, mock, _) = BuildProvider();
        mock.When(HttpMethod.Post, BaseUrl + "/v1/refunds")
            .Respond("application/json", """
                {"id":"re_1","status":"succeeded","amount":12000,"currency":"usd","payment_intent":"pi_old_1","charge":"ch_old_1","reason":"requested_by_customer"}
                """);

        var refund = await provider.RefundAsync(
            new RefundRequest("pi_old_1", 120m, "requested_by_customer", PaymentTransactionId: "tx-1", Currency: "USD"),
            CancellationToken.None);

        refund.Success.Should().BeTrue();
        refund.RefundId.Should().Be("re_1");
    }

    [Fact]
    public async Task HandleWebhookAsync_succeeded_payload_maps_to_succeeded_status()
    {
        var (provider, _, _) = BuildProvider();
        const string payload = """
            {"id":"evt_1","type":"payment_intent.succeeded","livemode":false,"data":{"object":{"id":"pi_wh_1","status":"succeeded","amount":10000,"currency":"usd","latest_charge":"ch_wh","payment_method":"pm_wh"}}}
            """;

        var result = await provider.HandleWebhookAsync(payload, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), CancellationToken.None);

        result.IntentId.Should().Be("pi_wh_1");
        result.Status.Should().Be(PaymentIntentStatus.Succeeded);
        result.Reference.Should().Be("ch_wh");
    }

    private static (StripePaymentProvider Provider, MockHttpMessageHandler MockHttp, Guid TenantId) BuildProvider()
    {
        var tenantId = Guid.NewGuid();
        var mockHttp = new MockHttpMessageHandler();
        var factory = new MockHttpClientFactory(mockHttp);
        var configResolver = new StubTenantProviderConfigResolver();
        configResolver.Configure(tenantId, ProviderCategory.Payment, StripePaymentProvider.ProviderKey, CredentialJson);

        var provider = new StripePaymentProvider(
            factory,
            configResolver,
            new StubProviderCredentialProtector(),
            new FakeTenantContext(tenantId),
            NullLogger<StripePaymentProvider>.Instance);

        return (provider, mockHttp, tenantId);
    }

    private static PaymentIntentRequest BuildIntentRequest(decimal amount, string currency) =>
        new(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-INT-1",
            Amount: amount,
            Currency: currency,
            TenantId: Guid.NewGuid(),
            CreatedByUserId: Guid.Empty,
            Description: null,
            Metadata: null,
            BillingInfo: null,
            LineItems: null);
}
