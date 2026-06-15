using CoreAlign.Application.B2B;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Identity.PersonaPreference.Commands;

public sealed class SetUserPreferencesHandler : IRequestHandler<SetUserPreferencesCommand, UserPreferenceSnapshot>
{
    private readonly IPersonaPreferenceService _personaPreferenceService;
    private readonly IUserPreferencesRepository _userPreferencesRepository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantContext _tenantContext;

    public SetUserPreferencesHandler(
        IPersonaPreferenceService personaPreferenceService,
        IUserPreferencesRepository userPreferencesRepository,
        ICurrentUserAccessor currentUser,
        ITenantContext tenantContext)
    {
        _personaPreferenceService = personaPreferenceService;
        _userPreferencesRepository = userPreferencesRepository;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<UserPreferenceSnapshot> Handle(SetUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserIdOrThrow();
        var tenantId = _tenantContext.RequireTenantId();

        var prefs = await _userPreferencesRepository.GetByUserAsync(userId, cancellationToken);
        if (prefs is null)
        {
            prefs = new UserPreferences(userId, tenantId);
            prefs.SetMode(request.Mode);
            prefs.SetLocaleOverride(request.LocaleOverride);
            prefs.SetThemeOverride(request.ThemeOverride);
            prefs.SetPerScreenOverrides(request.PerScreenOverridesJson);
            await _userPreferencesRepository.AddAsync(prefs, cancellationToken);
        }
        else
        {
            prefs.SetMode(request.Mode);
            prefs.SetLocaleOverride(request.LocaleOverride);
            prefs.SetThemeOverride(request.ThemeOverride);
            prefs.SetPerScreenOverrides(request.PerScreenOverridesJson);
            _userPreferencesRepository.Update(prefs);
        }

        return await _personaPreferenceService.GetSnapshotAsync(userId, tenantId, cancellationToken);
    }
}
