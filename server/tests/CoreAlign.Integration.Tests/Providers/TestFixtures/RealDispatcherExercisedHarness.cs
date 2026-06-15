using CoreAlign.Application.Common.Audit;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Providers.Payment;
using CoreAlign.Infrastructure.Providers.Payment.Stripe;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>
/// Real-class integration harness — wires the production <see cref="PaymentDispatcher"/>
/// against a stub HTTP server and in-memory repositories. Replaces the mock-shape
/// contract tests that exercised a fake dispatcher, so behavioral regressions in the
/// dispatcher's persistence / outbox / audit pipeline surface immediately.
/// </summary>
public sealed class RealDispatcherExercisedHarness
{
    public Guid TenantId { get; }
    public MockHttpMessageHandler MockHttp { get; }
    public StripePaymentProvider RealStripeProvider { get; }
    public IPaymentDispatcher RealDispatcher { get; }
    public InMemoryPaymentTransactionRepository TransactionRepository { get; }
    public InMemoryOutboxRepository OutboxRepository { get; }
    public InMemoryAuditContext AuditContext { get; }
    public InMemoryTenantProviderConfigRepository ConfigRepository { get; }
    public IOutboxSignal OutboxSignal { get; }
    public StubTenantProviderConfigResolver ConfigResolver { get; }
    public NoopUnitOfWork UnitOfWork { get; private set; } = new();

    public RealDispatcherExercisedHarness()
    {
        TenantId = Guid.NewGuid();
        MockHttp = new MockHttpMessageHandler();

        var httpClientFactory = new MockHttpClientFactory(MockHttp);
        var tenantContext = new FakeTenantContext(TenantId);
        var credentialProtector = new StubProviderCredentialProtector();

        ConfigResolver = new StubTenantProviderConfigResolver();
        var credentialJson = """
            {"SecretKey":"sk_test_integration","WebhookSigningSecret":"whsec_test","AccountId":null,"IsSandbox":true}
            """;
        ConfigResolver.Configure(TenantId, ProviderCategory.Payment, StripePaymentProvider.ProviderKey, credentialJson);

        RealStripeProvider = new StripePaymentProvider(
            httpClientFactory,
            ConfigResolver,
            credentialProtector,
            tenantContext,
            NullLogger<StripePaymentProvider>.Instance);

        var registry = new InMemoryProviderRegistry<IPaymentProvider>(RealStripeProvider);

        ConfigRepository = new InMemoryTenantProviderConfigRepository();
        ConfigRepository.Add(TenantId, ProviderCategory.Payment, StripePaymentProvider.ProviderKey);

        TransactionRepository = new InMemoryPaymentTransactionRepository();
        OutboxRepository = new InMemoryOutboxRepository();
        AuditContext = new InMemoryAuditContext();
        OutboxSignal = new OutboxSignal();

        UnitOfWork = new NoopUnitOfWork();
        RealDispatcher = new PaymentDispatcher(
            registry,
            ConfigRepository,
            TransactionRepository,
            OutboxRepository,
            OutboxSignal,
            AuditContext,
            tenantContext,
            UnitOfWork,
            NullLogger<PaymentDispatcher>.Instance);
    }
}
