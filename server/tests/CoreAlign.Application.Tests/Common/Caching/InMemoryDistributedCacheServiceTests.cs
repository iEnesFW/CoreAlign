using CoreAlign.Application.Common.Caching;
using CoreAlign.Infrastructure.Caching;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Common.Caching;

public class InMemoryDistributedCacheServiceTests
{
    private static InMemoryDistributedCacheService CreateSut(CacheRegionOptions? overrides = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        var opts = Options.Create(overrides ?? new CacheRegionOptions());
        return new InMemoryDistributedCacheService(cache, opts);
    }

    [Fact]
    public void BuildKey_emits_region_tenant_suffix_layout()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        var key = sut.BuildKey(nameof(CacheRegion.Dashboard), tenantId, "stats");

        key.Should().Be($"Dashboard:{tenantId:N}:stats");
    }

    [Fact]
    public void ResolveTtl_returns_region_default_when_not_supplied()
    {
        var sut = CreateSut(new CacheRegionOptions { DashboardTtlSeconds = 30, LookupsTtlSeconds = 300, CustomReportDataTtlSeconds = 60 });

        sut.ResolveTtl(nameof(CacheRegion.Dashboard), null).Should().Be(TimeSpan.FromSeconds(30));
        sut.ResolveTtl(nameof(CacheRegion.Lookups), null).Should().Be(TimeSpan.FromSeconds(300));
        sut.ResolveTtl(nameof(CacheRegion.CustomReportData), null).Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void ResolveTtl_honours_explicit_request()
    {
        var sut = CreateSut();
        sut.ResolveTtl(nameof(CacheRegion.Dashboard), TimeSpan.FromSeconds(7))
            .Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task GetOrAddAsync_returns_cached_object_on_second_call_within_ttl()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var key = sut.BuildKey(nameof(CacheRegion.Dashboard), tenantId, "stats");
        var factoryCalls = 0;

        Task<string> Factory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult($"value-{factoryCalls}");
        }

        var first = await sut.GetOrAddAsync(nameof(CacheRegion.Dashboard), key, Factory);
        var second = await sut.GetOrAddAsync(nameof(CacheRegion.Dashboard), key, Factory);

        first.Should().Be("value-1");
        second.Should().Be("value-1");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrAddAsync_isolates_tenants_using_the_same_logical_suffix()
    {
        var sut = CreateSut();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var keyA = sut.BuildKey(nameof(CacheRegion.Dashboard), tenantA, "stats");
        var keyB = sut.BuildKey(nameof(CacheRegion.Dashboard), tenantB, "stats");

        await sut.GetOrAddAsync(nameof(CacheRegion.Dashboard), keyA, _ => Task.FromResult("A"));
        await sut.GetOrAddAsync(nameof(CacheRegion.Dashboard), keyB, _ => Task.FromResult("B"));

        var fromA = await sut.GetAsync<string>(nameof(CacheRegion.Dashboard), keyA);
        var fromB = await sut.GetAsync<string>(nameof(CacheRegion.Dashboard), keyB);

        fromA.Should().Be("A");
        fromB.Should().Be("B");
        keyA.Should().NotBe(keyB);
    }

    [Fact]
    public async Task RemoveByTenantAsync_evicts_only_target_tenant_entries()
    {
        var sut = CreateSut();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var keyA1 = sut.BuildKey(nameof(CacheRegion.Dashboard), tenantA, "stats");
        var keyA2 = sut.BuildKey(nameof(CacheRegion.Lookups), tenantA, "countries");
        var keyB1 = sut.BuildKey(nameof(CacheRegion.Dashboard), tenantB, "stats");

        await sut.SetAsync(nameof(CacheRegion.Dashboard), keyA1, "a1");
        await sut.SetAsync(nameof(CacheRegion.Lookups), keyA2, "a2");
        await sut.SetAsync(nameof(CacheRegion.Dashboard), keyB1, "b1");

        await sut.RemoveByTenantAsync(tenantA);

        (await sut.GetAsync<string>(nameof(CacheRegion.Dashboard), keyA1)).Should().BeNull();
        (await sut.GetAsync<string>(nameof(CacheRegion.Lookups), keyA2)).Should().BeNull();
        (await sut.GetAsync<string>(nameof(CacheRegion.Dashboard), keyB1)).Should().Be("b1");
    }

    [Fact]
    public async Task RemoveByPrefixAsync_evicts_matching_keys_within_tenant_scope()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var matchA = sut.BuildKey(nameof(CacheRegion.Lookups), tenantId, "countries:tr");
        var matchB = sut.BuildKey(nameof(CacheRegion.Lookups), tenantId, "countries:en");
        var keep = sut.BuildKey(nameof(CacheRegion.Lookups), tenantId, "provinces:tr");

        await sut.SetAsync(nameof(CacheRegion.Lookups), matchA, "tr");
        await sut.SetAsync(nameof(CacheRegion.Lookups), matchB, "en");
        await sut.SetAsync(nameof(CacheRegion.Lookups), keep, "p");

        await sut.RemoveByPrefixAsync(nameof(CacheRegion.Lookups), tenantId, "countries");

        (await sut.GetAsync<string>(nameof(CacheRegion.Lookups), matchA)).Should().BeNull();
        (await sut.GetAsync<string>(nameof(CacheRegion.Lookups), matchB)).Should().BeNull();
        (await sut.GetAsync<string>(nameof(CacheRegion.Lookups), keep)).Should().Be("p");
    }

    [Fact]
    public async Task SetAsync_with_short_ttl_evicts_after_expiry()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var key = sut.BuildKey(nameof(CacheRegion.Generic), tenantId, "x");

        await sut.SetAsync(nameof(CacheRegion.Generic), key, "hit", TimeSpan.FromMilliseconds(50));
        (await sut.GetAsync<string>(nameof(CacheRegion.Generic), key)).Should().Be("hit");

        await Task.Delay(120);

        (await sut.GetAsync<string>(nameof(CacheRegion.Generic), key)).Should().BeNull();
    }

    [Fact]
    public void EnsureKeyShape_rejects_keys_built_outside_BuildKey()
    {
        var sut = CreateSut();

        var act = () => sut.GetAsync<string>(nameof(CacheRegion.Dashboard), "raw-key").GetAwaiter().GetResult();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task RemoveAsync_drops_a_single_key()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var key = sut.BuildKey(nameof(CacheRegion.Generic), tenantId, "single");

        await sut.SetAsync(nameof(CacheRegion.Generic), key, "v");
        await sut.RemoveAsync(nameof(CacheRegion.Generic), key);

        (await sut.GetAsync<string>(nameof(CacheRegion.Generic), key)).Should().BeNull();
    }

    [Fact]
    public async Task RemoveByRegionTenantAsync_does_not_evict_other_regions_for_same_tenant()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var dashboardKey = sut.BuildKey(nameof(CacheRegion.Dashboard), tenantId, "stats");
        var lookupsKey = sut.BuildKey(nameof(CacheRegion.Lookups), tenantId, "countries");
        var reportKey = sut.BuildKey(nameof(CacheRegion.CustomReportData), tenantId, "sales-q1");

        await sut.SetAsync(nameof(CacheRegion.Dashboard), dashboardKey, "d");
        await sut.SetAsync(nameof(CacheRegion.Lookups), lookupsKey, "l");
        await sut.SetAsync(nameof(CacheRegion.CustomReportData), reportKey, "r");

        await sut.RemoveByRegionTenantAsync(nameof(CacheRegion.Dashboard), tenantId);

        (await sut.GetAsync<string>(nameof(CacheRegion.Dashboard), dashboardKey)).Should().BeNull();
        (await sut.GetAsync<string>(nameof(CacheRegion.Lookups), lookupsKey)).Should().Be("l");
        (await sut.GetAsync<string>(nameof(CacheRegion.CustomReportData), reportKey)).Should().Be("r");
    }
}
