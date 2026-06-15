using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Identity.PersonaPreference;

/// <summary>
/// Resolves the effective UX complexity mode (Simple/Pro) for a user by chaining
/// the user-level override over the tenant default, with a hard fallback to
/// <see cref="UxComplexityMode.Simple"/> when neither is set. Persona switching
/// is global across the app and is consumed by the frontend through the
/// <c>users/me/preferences</c> endpoints.
/// </summary>
public interface IPersonaPreferenceService
{
    Task<UxComplexityMode> ResolveAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    Task SetUserOverrideAsync(Guid userId, Guid tenantId, UxComplexityMode? mode, CancellationToken cancellationToken = default);

    Task SetTenantDefaultAsync(Guid tenantId, UxComplexityMode mode, CancellationToken cancellationToken = default);

    Task<UserPreferenceSnapshot> GetSnapshotAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record UserPreferenceSnapshot(
    UxComplexityMode EffectiveMode,
    UxComplexityMode? UserOverride,
    UxComplexityMode TenantDefault,
    string? LocaleOverride,
    string? ThemeOverride,
    string? PerScreenOverridesJson);
