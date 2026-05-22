namespace CoreAlign.Domain.Interfaces;

public interface ITenantContext
{
    Guid? CurrentTenantId { get; }
    bool HasTenant { get; }

    /// <summary>
    /// Returns the current tenant id or throws <see cref="Exceptions.MissingTenantContextException"/>
    /// when no authenticated tenant scope is in effect. Use this in handlers that must not
    /// rely on the DbContext global query filter as the sole defense.
    /// </summary>
    Guid RequireTenantId();

    /// <summary>
    /// Asserts that <paramref name="resourceTenantId"/> matches the current tenant; throws
    /// <see cref="Exceptions.CrossTenantAccessException"/> otherwise. Defense-in-depth for
    /// handlers that load entities by id without an automatic filter (e.g. via raw SQL or
    /// IgnoreQueryFilters).
    /// </summary>
    void EnsureSameTenant(Guid resourceTenantId);
}
