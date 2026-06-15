using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Providers;

public class UpsertTenantProviderConfigHandlerTests
{
    private readonly ITenantProviderConfigRepository _repository = Substitute.For<ITenantProviderConfigRepository>();
    private readonly IProviderCredentialProtector _protector = Substitute.For<IProviderCredentialProtector>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ITenantProviderConfigResolver _resolver = Substitute.For<ITenantProviderConfigResolver>();

    private readonly Guid _tenantId = Guid.NewGuid();

    public UpsertTenantProviderConfigHandlerTests()
    {
        _tenantContext.RequireTenantId().Returns(_tenantId);
        _repository
            .ListByTenantAsync(_tenantId, Arg.Any<ProviderCategory?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TenantProviderConfig>());
    }

    private UpsertTenantProviderConfigHandler BuildSut() =>
        new(_repository, _protector, _tenantContext, _resolver);

    private static UpsertTenantProviderConfigCommand BuildCommand(
        ProviderCategory category = ProviderCategory.EFatura,
        string providerName = "nilvera",
        bool isDefault = false,
        bool isEnabled = true,
        string? plaintextCredentialsJson = null,
        int capabilities = 0) =>
        new(category, providerName, "Display", isDefault, isEnabled, plaintextCredentialsJson, capabilities);

    [Fact]
    public async Task Handle_creates_new_config_when_none_exists()
    {
        _repository
            .GetByTenantAndCategoryAsync(_tenantId, ProviderCategory.EFatura, "nilvera", Arg.Any<CancellationToken>())
            .Returns((TenantProviderConfig?)null);
        var sut = BuildSut();

        var dto = await sut.Handle(BuildCommand(), CancellationToken.None);

        dto.ProviderName.Should().Be("nilvera");
        await _repository.Received(1).AddAsync(
            Arg.Is<TenantProviderConfig>(c =>
                c.TenantId == _tenantId
                && c.ProviderName == "nilvera"
                && c.Category == ProviderCategory.EFatura),
            Arg.Any<CancellationToken>());
        _repository.DidNotReceive().Update(Arg.Any<TenantProviderConfig>());
    }

    [Fact]
    public async Task Handle_updates_existing_config_via_repository_update()
    {
        var existing = new TenantProviderConfig(
            ProviderCategory.EFatura,
            "nilvera",
            displayName: "Old",
            isDefault: false,
            isEnabled: false)
        {
            TenantId = _tenantId,
        };
        _repository
            .GetByTenantAndCategoryAsync(_tenantId, ProviderCategory.EFatura, "nilvera", Arg.Any<CancellationToken>())
            .Returns(existing);
        var sut = BuildSut();

        var dto = await sut.Handle(BuildCommand(isEnabled: true), CancellationToken.None);

        dto.IsEnabled.Should().BeTrue();
        existing.IsEnabled.Should().BeTrue();
        existing.DisplayName.Should().Be("Display");
        await _repository.DidNotReceive().AddAsync(Arg.Any<TenantProviderConfig>(), Arg.Any<CancellationToken>());
        _repository.Received().Update(existing);
    }

    [Fact]
    public async Task Handle_marks_sibling_defaults_as_non_default_when_setting_default()
    {
        var sibling = new TenantProviderConfig(
            ProviderCategory.EFatura,
            "other",
            isDefault: true)
        {
            TenantId = _tenantId,
        };
        _repository
            .GetByTenantAndCategoryAsync(_tenantId, ProviderCategory.EFatura, "nilvera", Arg.Any<CancellationToken>())
            .Returns((TenantProviderConfig?)null);
        _repository
            .ListByTenantAsync(_tenantId, ProviderCategory.EFatura, Arg.Any<CancellationToken>())
            .Returns(new[] { sibling });
        var sut = BuildSut();

        await sut.Handle(BuildCommand(isDefault: true), CancellationToken.None);

        sibling.IsDefault.Should().BeFalse();
        _repository.Received().Update(sibling);
    }

    [Fact]
    public async Task Handle_encrypts_plaintext_credentials_before_persisting()
    {
        _repository
            .GetByTenantAndCategoryAsync(_tenantId, ProviderCategory.EFatura, "nilvera", Arg.Any<CancellationToken>())
            .Returns((TenantProviderConfig?)null);
        _protector
            .Protect(_tenantId, ProviderCategory.EFatura, "{\"k\":\"v\"}")
            .Returns("enc::abc");
        var sut = BuildSut();

        await sut.Handle(
            BuildCommand(plaintextCredentialsJson: "{\"k\":\"v\"}"),
            CancellationToken.None);

        _protector.Received(1).Protect(_tenantId, ProviderCategory.EFatura, "{\"k\":\"v\"}");
        await _repository.Received(1).AddAsync(
            Arg.Is<TenantProviderConfig>(c => c.EncryptedCredentialsJson == "enc::abc"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_skips_protector_when_plaintext_credentials_missing()
    {
        _repository
            .GetByTenantAndCategoryAsync(_tenantId, ProviderCategory.EFatura, "nilvera", Arg.Any<CancellationToken>())
            .Returns((TenantProviderConfig?)null);
        var sut = BuildSut();

        await sut.Handle(BuildCommand(plaintextCredentialsJson: null), CancellationToken.None);

        _protector.DidNotReceive().Protect(Arg.Any<Guid>(), Arg.Any<ProviderCategory>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_invalidates_resolver_cache_after_save()
    {
        _repository
            .GetByTenantAndCategoryAsync(_tenantId, ProviderCategory.EFatura, "nilvera", Arg.Any<CancellationToken>())
            .Returns((TenantProviderConfig?)null);
        var sut = BuildSut();

        await sut.Handle(BuildCommand(), CancellationToken.None);

        await _resolver.Received(1).InvalidateCacheAsync(_tenantId, ProviderCategory.EFatura);
    }
}
