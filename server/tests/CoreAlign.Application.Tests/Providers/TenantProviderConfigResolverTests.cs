using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Tests.Providers;

public class TenantProviderConfigResolverTests
{
    private readonly ITenantProviderConfigRepository _repository = Substitute.For<ITenantProviderConfigRepository>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1024 });

    private TenantProviderConfigResolver BuildSut()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_repository);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        return new TenantProviderConfigResolver(scopeFactory, _cache);
    }

    private static TenantProviderConfig BuildConfig(
        Guid tenantId,
        ProviderCategory category,
        string providerName,
        bool isEnabled = true,
        bool isDefault = true)
    {
        var config = new TenantProviderConfig(category, providerName, isDefault: isDefault, isEnabled: isEnabled)
        {
            TenantId = tenantId,
        };
        return config;
    }

    [Fact]
    public async Task GetDefaultProviderNameAsync_returns_repository_value_on_cache_miss()
    {
        var tenantId = Guid.NewGuid();
        var stored = BuildConfig(tenantId, ProviderCategory.EFatura, "nilvera");
        _repository
            .GetDefaultForTenantAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns(stored);
        var sut = BuildSut();

        var result = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);

        result.Should().Be("nilvera");
        await _repository.Received(1).GetDefaultForTenantAsync(
            tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDefaultProviderNameAsync_returns_cached_value_on_second_call()
    {
        var tenantId = Guid.NewGuid();
        var stored = BuildConfig(tenantId, ProviderCategory.EFatura, "nilvera");
        _repository
            .GetDefaultForTenantAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns(stored);
        var sut = BuildSut();

        var first = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);
        var second = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);

        first.Should().Be("nilvera");
        second.Should().Be("nilvera");
        await _repository.Received(1).GetDefaultForTenantAsync(
            tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDefaultProviderNameAsync_returns_null_when_config_disabled()
    {
        var tenantId = Guid.NewGuid();
        var stored = BuildConfig(tenantId, ProviderCategory.EFatura, "nilvera", isEnabled: false);
        _repository
            .GetDefaultForTenantAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns(stored);
        var sut = BuildSut();

        var result = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);

        result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidateCacheAsync_clears_cache_so_next_call_hits_repository()
    {
        var tenantId = Guid.NewGuid();
        var stored = BuildConfig(tenantId, ProviderCategory.EFatura, "nilvera");
        _repository
            .GetDefaultForTenantAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns(stored);
        var sut = BuildSut();

        _ = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);
        await sut.InvalidateCacheAsync(tenantId, ProviderCategory.EFatura);
        _ = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);

        await _repository.Received(2).GetDefaultForTenantAsync(
            tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateCacheAsync_without_category_clears_every_category()
    {
        var tenantId = Guid.NewGuid();
        var efatura = BuildConfig(tenantId, ProviderCategory.EFatura, "nilvera");
        var payment = BuildConfig(tenantId, ProviderCategory.Payment, "iyzico");
        _repository
            .GetDefaultForTenantAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns(efatura);
        _repository
            .GetDefaultForTenantAsync(tenantId, ProviderCategory.Payment, Arg.Any<CancellationToken>())
            .Returns(payment);
        var sut = BuildSut();

        _ = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);
        _ = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.Payment);
        await sut.InvalidateCacheAsync(tenantId);
        _ = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura);
        _ = await sut.GetDefaultProviderNameAsync(tenantId, ProviderCategory.Payment);

        await _repository.Received(2).GetDefaultForTenantAsync(
            tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>());
        await _repository.Received(2).GetDefaultForTenantAsync(
            tenantId, ProviderCategory.Payment, Arg.Any<CancellationToken>());
    }
}
