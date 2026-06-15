namespace CoreAlign.Application.Billing;

/// <summary>
/// Resolves whether the current tenant has a given module currently active.
/// Implementations should cache the per-tenant module set for the duration of a
/// request (call lifetime) to keep <see cref="IsActiveAsync"/> O(1) when used
/// from <c>[RequireModule]</c> attributes on hot endpoints.
/// </summary>
public interface IActiveModulesService
{
    Task<bool> IsActiveAsync(string moduleCode, CancellationToken cancellationToken = default);

    /// <summary>All currently-active module codes for the current tenant.</summary>
    Task<IReadOnlySet<string>> GetActiveCodesAsync(CancellationToken cancellationToken = default);
}
