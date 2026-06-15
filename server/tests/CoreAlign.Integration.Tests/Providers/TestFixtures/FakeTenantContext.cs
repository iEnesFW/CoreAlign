using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>
/// Minimal in-memory <see cref="ITenantContext"/> for provider integration tests.
/// Hard-binds a single tenant so dispatcher / provider calls do not need a full HTTP scope.
/// </summary>
public sealed class FakeTenantContext : ITenantContext
{
    public FakeTenantContext(Guid tenantId)
    {
        CurrentTenantId = tenantId;
    }

    public Guid? CurrentTenantId { get; }
    public bool HasTenant => CurrentTenantId.HasValue;

    public Guid RequireTenantId() =>
        CurrentTenantId ?? throw new InvalidOperationException("No tenant scope.");

    public void EnsureSameTenant(Guid resourceTenantId)
    {
        if (resourceTenantId != CurrentTenantId)
        {
            throw new InvalidOperationException("Cross-tenant access denied.");
        }
    }

    public IDisposable PushScope(Guid tenantId) => new NoopScope();

    private sealed class NoopScope : IDisposable { public void Dispose() { } }
}
