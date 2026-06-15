using CoreAlign.Application.Providers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Providers;

public class ProviderRegistryTests
{
    private interface IEFaturaProvider : IExternalProvider { }

    private sealed class TestProviderA : IEFaturaProvider
    {
        public string Name => "test-a";
        public string DisplayName => "Test A";
        public ProviderCapabilities Capabilities => ProviderCapabilities.Empty;
    }

    private sealed class TestProviderB : IEFaturaProvider
    {
        public string Name => "test-b";
        public string DisplayName => "Test B";
        public ProviderCapabilities Capabilities => ProviderCapabilities.Empty;
    }

    private sealed class TestProviderC : IEFaturaProvider
    {
        public string Name => "test-c";
        public string DisplayName => "Test C";
        public ProviderCapabilities Capabilities => ProviderCapabilities.Empty;
    }

    private readonly ITenantProviderConfigResolver _resolver = Substitute.For<ITenantProviderConfigResolver>();
    private readonly TestProviderA _a = new();
    private readonly TestProviderB _b = new();
    private readonly TestProviderC _c = new();

    private ProviderRegistry<IEFaturaProvider> BuildSut() =>
        new(new IEFaturaProvider[] { _a, _b, _c }, _resolver);

    [Fact]
    public void Find_returns_provider_when_name_matches_case_insensitively()
    {
        var sut = BuildSut();

        sut.Find("test-a").Should().BeSameAs(_a);
        sut.Find("TEST-A").Should().BeSameAs(_a);
    }

    [Fact]
    public void Require_throws_when_provider_unknown()
    {
        var sut = BuildSut();

        var act = () => sut.Require("unknown");
        act.Should().Throw<ProviderNotFoundException>()
            .Which.ProviderName.Should().Be("unknown");
    }

    [Fact]
    public void All_returns_every_registered_provider()
    {
        var sut = BuildSut();

        sut.All.Should().HaveCount(3);
        sut.All.Should().Contain(new IEFaturaProvider[] { _a, _b, _c });
    }

    [Fact]
    public void Names_lists_every_registered_provider_name()
    {
        var sut = BuildSut();

        sut.Names.Should().BeEquivalentTo(new[] { "test-a", "test-b", "test-c" });
    }

    [Fact]
    public async Task ResolveForTenantAsync_returns_provider_configured_for_tenant()
    {
        var tenantId = Guid.NewGuid();
        _resolver
            .GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns("test-b");
        var sut = BuildSut();

        var provider = await sut.ResolveForTenantAsync(tenantId);

        provider.Should().BeSameAs(_b);
    }

    [Fact]
    public async Task ResolveForTenantAsync_throws_when_provider_not_configured()
    {
        var tenantId = Guid.NewGuid();
        _resolver
            .GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var sut = BuildSut();

        var act = async () => await sut.ResolveForTenantAsync(tenantId);

        var thrown = await act.Should().ThrowAsync<ProviderNotConfiguredException>();
        thrown.Which.TenantId.Should().Be(tenantId);
        thrown.Which.Category.Should().Be(ProviderCategory.EFatura);
    }

    [Fact]
    public async Task TryResolveForTenantAsync_returns_null_when_provider_not_configured()
    {
        var tenantId = Guid.NewGuid();
        _resolver
            .GetDefaultProviderNameAsync(tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var sut = BuildSut();

        var provider = await sut.TryResolveForTenantAsync(tenantId);

        provider.Should().BeNull();
    }
}
