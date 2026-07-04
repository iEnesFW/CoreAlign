using CoreAlign.Application.Fx;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Fx;

public class ResolveFxRateHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IFxRateResolverDetailed _resolver = Substitute.For<IFxRateResolverDetailed>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private ResolveFxRateHandler BuildHandler()
    {
        _tenantContext.CurrentTenantId.Returns(TenantId);
        return new ResolveFxRateHandler(_resolver, _tenantContext);
    }

    [Fact]
    public async Task Unspecified_kind_as_of_date_is_normalized_to_utc_before_resolution()
    {
        var unspecified = new DateTime(2026, 7, 3);
        unspecified.Kind.Should().Be(DateTimeKind.Unspecified);
        var handler = BuildHandler();

        await handler.Handle(new ResolveFxRateQuery("USD", unspecified), CancellationToken.None);

        await _resolver.Received(1).ResolveDetailedAsync(
            "USD",
            Arg.Is<DateTime>(d => d.Kind == DateTimeKind.Utc && d.Ticks == unspecified.Ticks),
            TenantId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_as_of_date_defaults_to_current_utc_time()
    {
        var handler = BuildHandler();

        await handler.Handle(new ResolveFxRateQuery("EUR", null), CancellationToken.None);

        await _resolver.Received(1).ResolveDetailedAsync(
            "EUR",
            Arg.Is<DateTime>(d => d.Kind == DateTimeKind.Utc),
            TenantId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolved_snapshot_is_mapped_to_dto()
    {
        var effective = DateTime.SpecifyKind(new DateTime(2026, 7, 2), DateTimeKind.Utc);
        _resolver.ResolveDetailedAsync("USD", Arg.Any<DateTime>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(new FxResolutionResult(
                new FxRateSnapshot("USD", 32.5m, 32.7m, effective, "TCMB"),
                FxSource.Tcmb,
                UsedTenantOverride: false));
        var handler = BuildHandler();

        var dto = await handler.Handle(
            new ResolveFxRateQuery("USD", DateTime.SpecifyKind(new DateTime(2026, 7, 3), DateTimeKind.Utc)),
            CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.CurrencyCode.Should().Be("USD");
        dto.BuyingRate.Should().Be(32.5m);
        dto.SellingRate.Should().Be(32.7m);
        dto.EffectiveDate.Should().Be(effective);
        dto.UsedTenantOverride.Should().BeFalse();
    }

    [Fact]
    public async Task Unresolvable_currency_returns_null()
    {
        _resolver.ResolveDetailedAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((FxResolutionResult?)null);
        var handler = BuildHandler();

        var dto = await handler.Handle(new ResolveFxRateQuery("XXX", null), CancellationToken.None);

        dto.Should().BeNull();
    }
}
