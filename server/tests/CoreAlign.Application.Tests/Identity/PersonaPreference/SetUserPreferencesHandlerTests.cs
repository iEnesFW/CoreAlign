using CoreAlign.Application.B2B;
using CoreAlign.Application.Identity.PersonaPreference;
using CoreAlign.Application.Identity.PersonaPreference.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Identity.PersonaPreference;

public class SetUserPreferencesHandlerTests
{
    private readonly IPersonaPreferenceService _personaService = Substitute.For<IPersonaPreferenceService>();
    private readonly IUserPreferencesRepository _userPrefsRepo = Substitute.For<IUserPreferencesRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private SetUserPreferencesHandler CreateSut()
    {
        _currentUser.UserIdOrThrow().Returns(UserId);
        _tenantContext.RequireTenantId().Returns(TenantId);
        return new SetUserPreferencesHandler(_personaService, _userPrefsRepo, _currentUser, _tenantContext);
    }

    [Fact]
    public async Task Handle_creates_new_preferences_when_none_exist_and_returns_snapshot()
    {
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserPreferences?)null);

        var expected = new UserPreferenceSnapshot(
            UxComplexityMode.Simple,
            UxComplexityMode.Simple,
            UxComplexityMode.Pro,
            "en-US",
            "dark",
            null);
        _personaService.GetSnapshotAsync(UserId, TenantId, Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut();
        var command = new SetUserPreferencesCommand(UxComplexityMode.Simple, "en-US", "dark", null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _userPrefsRepo.Received(1).AddAsync(
            Arg.Is<UserPreferences>(p => p.UserId == UserId
                                          && p.TenantId == TenantId
                                          && p.ModeOverride == UxComplexityMode.Simple
                                          && p.LocaleOverride == "en-US"
                                          && p.ThemeOverride == "dark"),
            Arg.Any<CancellationToken>());
        _userPrefsRepo.DidNotReceive().Update(Arg.Any<UserPreferences>());
        await _personaService.Received(1).GetSnapshotAsync(UserId, TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_updates_existing_preferences_in_place_and_persists_via_repository()
    {
        var existing = new UserPreferences(UserId, TenantId);
        existing.SetMode(UxComplexityMode.Pro);
        existing.SetLocaleOverride("tr-TR");
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns(existing);

        var expected = new UserPreferenceSnapshot(
            UxComplexityMode.Simple,
            UxComplexityMode.Simple,
            UxComplexityMode.Pro,
            "en-US",
            null,
            "{\"orders\":\"Pro\"}");
        _personaService.GetSnapshotAsync(UserId, TenantId, Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut();
        var command = new SetUserPreferencesCommand(UxComplexityMode.Simple, "en-US", null, "{\"orders\":\"Pro\"}");

        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().BeSameAs(expected);
        existing.ModeOverride.Should().Be(UxComplexityMode.Simple);
        existing.LocaleOverride.Should().Be("en-US");
        existing.ThemeOverride.Should().BeNull();
        existing.PerScreenOverridesJson.Should().Be("{\"orders\":\"Pro\"}");
        _userPrefsRepo.Received(1).Update(existing);
        await _userPrefsRepo.DidNotReceive().AddAsync(Arg.Any<UserPreferences>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_uses_current_user_and_tenant_from_context_not_command_payload()
    {
        _userPrefsRepo.GetByUserAsync(UserId, Arg.Any<CancellationToken>()).Returns((UserPreferences?)null);
        _personaService.GetSnapshotAsync(UserId, TenantId, Arg.Any<CancellationToken>())
            .Returns(new UserPreferenceSnapshot(UxComplexityMode.Pro, null, UxComplexityMode.Pro, null, null, null));

        var sut = CreateSut();
        await sut.Handle(new SetUserPreferencesCommand(null, null, null, null), CancellationToken.None);

        _currentUser.Received().UserIdOrThrow();
        _tenantContext.Received().RequireTenantId();
        await _userPrefsRepo.Received(1).AddAsync(
            Arg.Is<UserPreferences>(p => p.UserId == UserId && p.TenantId == TenantId),
            Arg.Any<CancellationToken>());
    }
}
