using CoreAlign.Application.Common.Caching;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Caching;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Common.Caching;

public class DistributedLookupCacheServiceTests
{
    private sealed class StubTenant : ITenantContext
    {
        public StubTenant(Guid? tenantId) => CurrentTenantId = tenantId;
        public Guid? CurrentTenantId { get; }
        public bool HasTenant => CurrentTenantId.HasValue;
        public Guid RequireTenantId() => CurrentTenantId ?? throw new InvalidOperationException();
        public void EnsureSameTenant(Guid resourceTenantId)
        {
            if (resourceTenantId != CurrentTenantId) throw new InvalidOperationException();
        }
        public IDisposable PushScope(Guid tenantId) => new NoopScope();
        private sealed class NoopScope : IDisposable { public void Dispose() { } }
    }

    private static (DistributedLookupCacheService sut, IDistributedCacheService backing) CreateSut(Guid? tenantId)
    {
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        IDistributedCacheService backing = new InMemoryDistributedCacheService(cache, Options.Create(new CacheRegionOptions()));
        var sut = new DistributedLookupCacheService(backing, new StubTenant(tenantId));
        return (sut, backing);
    }

    [Fact]
    public async Task GetOrCreateAsync_caches_within_default_300s_ttl()
    {
        var (sut, _) = CreateSut(Guid.NewGuid());
        var calls = 0;

        var first = await sut.GetOrCreateAsync<string>("countries", _ =>
        {
            calls++;
            return Task.FromResult<string?>("TR,GB,US");
        });
        var second = await sut.GetOrCreateAsync<string>("countries", _ =>
        {
            calls++;
            return Task.FromResult<string?>("OVERWRITE");
        });

        first.Should().Be("TR,GB,US");
        second.Should().Be("TR,GB,US");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_isolates_tenants_using_the_same_logical_suffix()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });
        IDistributedCacheService backing = new InMemoryDistributedCacheService(cache, Options.Create(new CacheRegionOptions()));
        var sutA = new DistributedLookupCacheService(backing, new StubTenant(tenantA));
        var sutB = new DistributedLookupCacheService(backing, new StubTenant(tenantB));

        await sutA.GetOrCreateAsync<string>("countries", _ => Task.FromResult<string?>("A-data"));
        await sutB.GetOrCreateAsync<string>("countries", _ => Task.FromResult<string?>("B-data"));

        var sawFromA = await sutA.GetOrCreateAsync<string>("countries", _ => Task.FromResult<string?>("never"));
        var sawFromB = await sutB.GetOrCreateAsync<string>("countries", _ => Task.FromResult<string?>("never"));

        sawFromA.Should().Be("A-data");
        sawFromB.Should().Be("B-data");
    }

    [Fact]
    public async Task InvalidatePrefix_evicts_only_matching_keys_for_current_tenant()
    {
        var (sut, _) = CreateSut(Guid.NewGuid());
        var factoryHits = 0;

        await sut.GetOrCreateAsync<string>("countries:tr", _ => { factoryHits++; return Task.FromResult<string?>("tr"); });
        await sut.GetOrCreateAsync<string>("countries:en", _ => { factoryHits++; return Task.FromResult<string?>("en"); });
        await sut.GetOrCreateAsync<string>("provinces:tr", _ => { factoryHits++; return Task.FromResult<string?>("p"); });
        factoryHits.Should().Be(3);

        sut.InvalidatePrefix("countries");

        var trReload = await sut.GetOrCreateAsync<string>("countries:tr", _ => { factoryHits++; return Task.FromResult<string?>("tr-fresh"); });
        var provinces = await sut.GetOrCreateAsync<string>("provinces:tr", _ => { factoryHits++; return Task.FromResult<string?>("p-fresh"); });

        trReload.Should().Be("tr-fresh");
        provinces.Should().Be("p");
        factoryHits.Should().Be(4);
    }

    [Fact]
    public async Task GetOrCreateAsync_does_not_cache_null_factory_results()
    {
        var (sut, _) = CreateSut(Guid.NewGuid());
        var calls = 0;

        var first = await sut.GetOrCreateAsync<string>("missing", _ =>
        {
            calls++;
            return Task.FromResult<string?>(null);
        });
        var second = await sut.GetOrCreateAsync<string>("missing", _ =>
        {
            calls++;
            return Task.FromResult<string?>("now-present");
        });

        first.Should().BeNull();
        second.Should().Be("now-present");
        calls.Should().Be(2);
    }
}
