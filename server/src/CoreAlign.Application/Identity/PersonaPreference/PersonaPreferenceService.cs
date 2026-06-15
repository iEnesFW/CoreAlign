using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Identity.PersonaPreference;

public sealed class PersonaPreferenceService : IPersonaPreferenceService
{
    private readonly IUserPreferencesRepository _userPreferencesRepository;
    private readonly ITenantRepository _tenantRepository;

    public PersonaPreferenceService(
        IUserPreferencesRepository userPreferencesRepository,
        ITenantRepository tenantRepository)
    {
        _userPreferencesRepository = userPreferencesRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<UxComplexityMode> ResolveAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var prefs = await _userPreferencesRepository.GetByUserAsync(userId, cancellationToken);
        if (prefs?.ModeOverride is { } userMode)
        {
            return userMode;
        }

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        return tenant?.DefaultUxComplexityMode ?? UxComplexityMode.Simple;
    }

    public async Task SetUserOverrideAsync(Guid userId, Guid tenantId, UxComplexityMode? mode, CancellationToken cancellationToken = default)
    {
        var prefs = await _userPreferencesRepository.GetByUserAsync(userId, cancellationToken);
        if (prefs is null)
        {
            prefs = new UserPreferences(userId, tenantId);
            prefs.SetMode(mode);
            await _userPreferencesRepository.AddAsync(prefs, cancellationToken);
            return;
        }

        prefs.SetMode(mode);
        _userPreferencesRepository.Update(prefs);
    }

    public async Task SetTenantDefaultAsync(Guid tenantId, UxComplexityMode mode, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant '{tenantId}' was not found.");

        tenant.DefaultUxComplexityMode = mode;
        tenant.UpdatedAtUtc = DateTime.UtcNow;
        _tenantRepository.Update(tenant);
    }

    public async Task<UserPreferenceSnapshot> GetSnapshotAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var prefs = await _userPreferencesRepository.GetByUserAsync(userId, cancellationToken);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        var tenantDefault = tenant?.DefaultUxComplexityMode ?? UxComplexityMode.Simple;
        var effective = prefs?.ModeOverride ?? tenantDefault;

        return new UserPreferenceSnapshot(
            effective,
            prefs?.ModeOverride,
            tenantDefault,
            prefs?.LocaleOverride,
            prefs?.ThemeOverride,
            prefs?.PerScreenOverridesJson);
    }
}
