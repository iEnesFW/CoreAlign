using CoreAlign.Application.Common.Caching;
using CoreAlign.Infrastructure.Caching;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Common.Caching;

public class DistributedDashboardCacheServiceTests
{
    private static (DistributedDashboardCacheService sut, IDistributedCacheService backing) CreateSut()
    {
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        IDistributedCacheService backing = new InMemoryDistributedCacheService(cache, Options.Create(new CacheRegionOptions()));
        return (new DistributedDashboardCacheService(backing), backing);
    }

    [Fact]
    public void BuildKey_emits_dashboard_region_layout()
    {
        var (sut, _) = CreateSut();
        var tenantId = Guid.NewGuid();

        var key = sut.BuildKey(tenantId, "stats");

        key.Should().Be($"Dashboard:{tenantId:N}:stats");
    }

    [Fact]
    public async Task GetOrAddAsync_caches_second_call_within_30_second_default_ttl()
    {
        var (sut, _) = CreateSut();
        var tenantId = Guid.NewGuid();
        var key = sut.BuildKey(tenantId, "stats");
        var calls = 0;

        var first = await sut.GetOrAddAsync(key, _ =>
        {
            calls++;
            return Task.FromResult(new { Customers = 42 });
        });
        var second = await sut.GetOrAddAsync(key, _ =>
        {
            calls++;
            return Task.FromResult(new { Customers = 0 });
        });

        first.Customers.Should().Be(42);
        second.Customers.Should().Be(42);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task InvalidateTenant_drops_only_target_tenant_entries()
    {
        var (sut, _) = CreateSut();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var keyA = sut.BuildKey(tenantA, "stats");
        var keyB = sut.BuildKey(tenantB, "stats");

        await sut.GetOrAddAsync(keyA, _ => Task.FromResult("a"));
        await sut.GetOrAddAsync(keyB, _ => Task.FromResult("b"));

        sut.InvalidateTenant(tenantA);

        var afterA = await sut.GetOrAddAsync(keyA, _ => Task.FromResult("a-fresh"));
        var afterB = await sut.GetOrAddAsync(keyB, _ => Task.FromResult("b-fresh"));

        afterA.Should().Be("a-fresh");
        afterB.Should().Be("b");
    }

    [Fact]
    public async Task InvalidateTenant_does_not_evict_other_regions_for_same_tenant()
    {
        var (sut, backing) = CreateSut();
        var tenantId = Guid.NewGuid();
        var dashboardKey = sut.BuildKey(tenantId, "stats");
        var lookupsKey = backing.BuildKey(nameof(CacheRegion.Lookups), tenantId, "countries");

        await sut.GetOrAddAsync(dashboardKey, _ => Task.FromResult("dash"));
        await backing.SetAsync(nameof(CacheRegion.Lookups), lookupsKey, "lookups-value");

        sut.InvalidateTenant(tenantId);

        var dashboardAfter = await backing.GetAsync<string>(nameof(CacheRegion.Dashboard), dashboardKey);
        var lookupsAfter = await backing.GetAsync<string>(nameof(CacheRegion.Lookups), lookupsKey);

        dashboardAfter.Should().BeNull();
        lookupsAfter.Should().Be("lookups-value");
    }
}
