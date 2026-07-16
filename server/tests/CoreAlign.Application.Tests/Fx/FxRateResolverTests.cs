using CoreAlign.Application.Fx;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Entities.Treasury;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Fx;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Fx;

public class FxRateResolverTests
{
    private static readonly DateTime AsOf = DateTime.SpecifyKind(new DateTime(2026, 6, 4), DateTimeKind.Utc);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IExchangeRateRepository _exchangeRates = Substitute.For<IExchangeRateRepository>();
    private readonly ITenantFxPreferences _preferences = Substitute.For<ITenantFxPreferences>();

    [Fact]
    public async Task Tenant_override_wins_over_preferred_source_and_global()
    {
        _exchangeRates.GetLatestTenantOverridesOnOrBeforeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExchangeRate>
            {
                new()
                {
                    Currency = "USD",
                    RateAgainstTry = 99m,
                    ValidOnDate = AsOf,
                    Source = FxSourceCodes.TenantOverride,
                    TenantId = TenantId,
                },
            });

        var tcmb = StubProvider(FxSource.Tcmb, "USD", 32m);
        var ecb = StubProvider(FxSource.Ecb, "USD", 31m);
        _preferences.GetAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantFxPreferenceSnapshot(FxSource.Ecb, new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase)));

        var sut = BuildResolver(tcmb, ecb);

        var result = await sut.ResolveDetailedAsync("USD", AsOf, TenantId);

        result.Should().NotBeNull();
        result!.Source.Should().Be(FxSource.TenantOverride);
        result.UsedTenantOverride.Should().BeTrue();
        result.Snapshot.BuyingRate.Should().Be(99m);
        await tcmb.DidNotReceive().TryGetRateAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await ecb.DidNotReceive().TryGetRateAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tenant_pref_drives_preferred_provider_selection()
    {
        _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExchangeRate>());
        _preferences.GetAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantFxPreferenceSnapshot(FxSource.Ecb, new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase)));

        var tcmb = StubProvider(FxSource.Tcmb, "EUR", 35m);
        var ecb = StubProvider(FxSource.Ecb, "EUR", 36m);

        var sut = BuildResolver(tcmb, ecb);

        var result = await sut.ResolveDetailedAsync("EUR", AsOf, TenantId);

        result.Should().NotBeNull();
        result!.Source.Should().Be(FxSource.Ecb);
        result.UsedTenantOverride.Should().BeFalse();
        result.Snapshot.BuyingRate.Should().Be(36m);
        await ecb.Received(1).TryGetRateAsync("EUR", AsOf, Arg.Any<CancellationToken>());
        await tcmb.DidNotReceive().TryGetRateAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Per_currency_override_takes_precedence_over_default_pref()
    {
        _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExchangeRate>());
        var perCurrency = new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase)
        {
            ["EUR"] = FxSource.Ecb,
        };
        _preferences.GetAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantFxPreferenceSnapshot(FxSource.Tcmb, perCurrency));

        var tcmb = StubProvider(FxSource.Tcmb, "EUR", 35m);
        var ecb = StubProvider(FxSource.Ecb, "EUR", 36m);

        var sut = BuildResolver(tcmb, ecb);

        var result = await sut.ResolveDetailedAsync("EUR", AsOf, TenantId);

        result!.Source.Should().Be(FxSource.Ecb);
        result.Snapshot.BuyingRate.Should().Be(36m);
    }

    [Fact]
    public async Task Falls_back_to_tcmb_when_preferred_source_returns_null()
    {
        _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExchangeRate>());
        _preferences.GetAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantFxPreferenceSnapshot(FxSource.Ecb, new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase)));

        var tcmb = StubProvider(FxSource.Tcmb, "USD", 32m);
        var ecb = StubProvider(FxSource.Ecb, "USD", null);

        var sut = BuildResolver(tcmb, ecb);

        var result = await sut.ResolveDetailedAsync("USD", AsOf, TenantId);

        result.Should().NotBeNull();
        result!.Source.Should().Be(FxSource.Tcmb);
        result.Snapshot.BuyingRate.Should().Be(32m);
    }

    [Fact]
    public async Task Defaults_to_tcmb_when_tenant_is_null()
    {
        _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExchangeRate>());

        var tcmb = StubProvider(FxSource.Tcmb, "USD", 32m);
        var ecb = StubProvider(FxSource.Ecb, "USD", 31m);

        var sut = BuildResolver(tcmb, ecb);

        var result = await sut.ResolveDetailedAsync("USD", AsOf, tenantId: null);

        result.Should().NotBeNull();
        result!.Source.Should().Be(FxSource.Tcmb);
        result.Snapshot.BuyingRate.Should().Be(32m);
        await _preferences.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_returns_null_when_currency_code_is_blank()
    {
        var sut = BuildResolver(StubProvider(FxSource.Tcmb, "USD", 32m));

        var result = await sut.ResolveDetailedAsync(string.Empty, AsOf, TenantId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Resolve_returns_null_when_no_provider_supports_currency_and_no_fallback()
    {
        _exchangeRates.GetLatestPerCurrencyOnOrBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ExchangeRate>());
        _preferences.GetAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantFxPreferenceSnapshot(FxSource.Ecb, new Dictionary<string, FxSource>(StringComparer.OrdinalIgnoreCase)));

        var ecb = StubProvider(FxSource.Ecb, "USD", null);

        var sut = BuildResolver(ecb);

        var result = await sut.ResolveDetailedAsync("USD", AsOf, TenantId);

        result.Should().BeNull();
    }

    private FxRateResolver BuildResolver(params IFxSourceProvider[] providers)
    {
        var tenantOverride = new TenantOverrideFxProvider(_exchangeRates);
        return new FxRateResolver(providers, tenantOverride, _preferences, NullLogger<FxRateResolver>.Instance);
    }

    private static IFxSourceProvider StubProvider(FxSource source, string currency, decimal? rate)
    {
        var provider = Substitute.For<IFxSourceProvider>();
        provider.Source.Returns(source);
        provider.SupportsCurrency(Arg.Any<string>()).Returns(true);
        if (rate is null)
        {
            provider.TryGetRateAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns((FxRateSnapshot?)null);
        }
        else
        {
            provider.TryGetRateAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(new FxRateSnapshot(currency, rate.Value, rate.Value, AsOf, FxSourceCodes.ToCode(source)));
        }
        return provider;
    }
}
