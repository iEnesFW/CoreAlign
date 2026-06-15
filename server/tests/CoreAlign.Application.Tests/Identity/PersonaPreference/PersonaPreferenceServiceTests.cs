using CoreAlign.Application.Identity.PersonaPreference;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Identity.PersonaPreference;

public class PersonaPreferenceServiceTests
{
    private readonly IUserPreferencesRepository _userPrefsRepo = Substitute.For<IUserPreferencesRepository>();
    private readonly ITenantRepository _tenantRepo = Substitute.For<ITenantRepository>();
    private readonly PersonaPreferenceService _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    public PersonaPreferenceServiceTests()
    {
        _sut = new PersonaPreferenceService(_userPrefsRepo, _tenantRepo);
    }

    private Tenant CreateTenant(UxComplexityMode tenantDefault = UxComplexityMode.Pro)
    {
        var tenant = new Tenant("Acme", "acme") { Id = TenantId, DefaultUxComplexityMode = tenantDefault };
        _tenantRepo.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns(tenant);
        return tenant;
    }

    [Fact]
    public async Task Resolve_returns_tenant_default_when_user_override_is_null()
    {
        CreateTenant(UxComplexityMode.Pro);
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserPreferences?)null);

        var mode = await _sut.ResolveAsync(UserId, TenantId);

        mode.Should().Be(UxComplexityMode.Pro);
    }

    [Fact]
    public async Task Resolve_returns_user_override_when_set_even_if_tenant_default_differs()
    {
        CreateTenant(UxComplexityMode.Pro);
        var prefs = new UserPreferences(UserId, TenantId);
        prefs.SetMode(UxComplexityMode.Simple);
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns(prefs);

        var mode = await _sut.ResolveAsync(UserId, TenantId);

        mode.Should().Be(UxComplexityMode.Simple);
    }

    [Fact]
    public async Task SetUserOverride_creates_new_preferences_when_none_exist()
    {
        CreateTenant(UxComplexityMode.Pro);
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserPreferences?)null);

        await _sut.SetUserOverrideAsync(UserId, TenantId, UxComplexityMode.Simple);

        await _userPrefsRepo.Received(1).AddAsync(
            Arg.Is<UserPreferences>(p => p.UserId == UserId
                                          && p.TenantId == TenantId
                                          && p.ModeOverride == UxComplexityMode.Simple),
            Arg.Any<CancellationToken>());
        _userPrefsRepo.DidNotReceive().Update(Arg.Any<UserPreferences>());
    }

    [Fact]
    public async Task SetUserOverride_updates_existing_preferences_in_place()
    {
        CreateTenant(UxComplexityMode.Pro);
        var existing = new UserPreferences(UserId, TenantId);
        existing.SetMode(UxComplexityMode.Pro);
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.SetUserOverrideAsync(UserId, TenantId, UxComplexityMode.Simple);

        existing.ModeOverride.Should().Be(UxComplexityMode.Simple);
        _userPrefsRepo.Received(1).Update(existing);
        await _userPrefsRepo.DidNotReceive().AddAsync(Arg.Any<UserPreferences>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetTenantDefault_writes_mode_onto_tenant_aggregate()
    {
        var tenant = CreateTenant(UxComplexityMode.Pro);

        await _sut.SetTenantDefaultAsync(TenantId, UxComplexityMode.Simple);

        tenant.DefaultUxComplexityMode.Should().Be(UxComplexityMode.Simple);
        _tenantRepo.Received(1).Update(tenant);
    }

    [Fact]
    public async Task SetTenantDefault_throws_when_tenant_missing()
    {
        _tenantRepo.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = () => _sut.SetTenantDefaultAsync(TenantId, UxComplexityMode.Pro);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetSnapshot_returns_effective_user_override_and_tenant_default_together()
    {
        CreateTenant(UxComplexityMode.Pro);
        var prefs = new UserPreferences(UserId, TenantId);
        prefs.SetMode(UxComplexityMode.Simple);
        prefs.SetLocaleOverride("en-US");
        prefs.SetThemeOverride("dark");
        prefs.SetPerScreenOverrides("{\"orders\":\"Pro\"}");
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns(prefs);

        var snapshot = await _sut.GetSnapshotAsync(UserId, TenantId);

        snapshot.EffectiveMode.Should().Be(UxComplexityMode.Simple);
        snapshot.UserOverride.Should().Be(UxComplexityMode.Simple);
        snapshot.TenantDefault.Should().Be(UxComplexityMode.Pro);
        snapshot.LocaleOverride.Should().Be("en-US");
        snapshot.ThemeOverride.Should().Be("dark");
        snapshot.PerScreenOverridesJson.Should().Be("{\"orders\":\"Pro\"}");
    }

    [Fact]
    public async Task GetSnapshot_falls_back_to_simple_when_neither_user_nor_tenant_set()
    {
        _tenantRepo.GetByIdAsync(TenantId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserPreferences?)null);

        var snapshot = await _sut.GetSnapshotAsync(UserId, TenantId);

        snapshot.EffectiveMode.Should().Be(UxComplexityMode.Simple);
        snapshot.UserOverride.Should().BeNull();
        snapshot.TenantDefault.Should().Be(UxComplexityMode.Simple);
    }
}
