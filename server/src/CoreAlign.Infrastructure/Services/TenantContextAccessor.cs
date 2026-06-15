using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CoreAlign.Infrastructure.Services;

public class TenantContextAccessor : ITenantContext
{
    public const string TenantClaimType = "tenant_id";

    // Cache key for the parsed Guid in HttpContext.Items — the LoggingBehavior,
    // DbContext, and middleware each touch CurrentTenantId multiple times per
    // request; without this, every access re-parses the claim string.
    private const string CacheKey = "__tenant_id_parsed";
    private static readonly object Sentinel = new();

    // AsyncLocal override used by background work (PushScope) to inject a tenant
    // when there is no HttpContext. Always takes precedence over the claim lookup.
    private static readonly AsyncLocal<Guid?> AmbientScope = new();

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentTenantId
    {
        get
        {
            if (AmbientScope.Value.HasValue) return AmbientScope.Value;

            var ctx = _httpContextAccessor.HttpContext;
            // No HTTP context (background work / anonymous endpoints) → no tenant.
            if (ctx is null) return null;

            if (ctx.Items.TryGetValue(CacheKey, out var cached))
            {
                return ReferenceEquals(cached, Sentinel) ? null : (Guid?)cached;
            }

            // Only honor the claim if the user is authenticated; bare claims on an
            // un-authenticated principal should never grant tenant access.
            if (ctx.User.Identity?.IsAuthenticated != true)
            {
                ctx.Items[CacheKey] = Sentinel;
                return null;
            }

            var raw = ctx.User.FindFirst(TenantClaimType)?.Value;
            if (!Guid.TryParse(raw, out var id) || id == Guid.Empty)
            {
                ctx.Items[CacheKey] = Sentinel;
                return null;
            }

            ctx.Items[CacheKey] = id;
            return id;
        }
    }

    public bool HasTenant => CurrentTenantId.HasValue;

    public Guid RequireTenantId()
    {
        var id = CurrentTenantId;
        if (id is null) throw new MissingTenantContextException();
        return id.Value;
    }

    public void EnsureSameTenant(Guid resourceTenantId)
    {
        if (resourceTenantId == Guid.Empty) throw new CrossTenantAccessException();
        if (RequireTenantId() != resourceTenantId) throw new CrossTenantAccessException();
    }

    public IDisposable PushScope(Guid tenantId)
    {
        var prior = AmbientScope.Value;
        AmbientScope.Value = tenantId == Guid.Empty ? null : tenantId;
        return new ScopePopper(prior);
    }

    public static IDisposable PushTenant(Guid tenantId)
    {
        var prior = AmbientScope.Value;
        AmbientScope.Value = tenantId == Guid.Empty ? null : tenantId;
        return new ScopePopper(prior);
    }

    private sealed class ScopePopper : IDisposable
    {
        private readonly Guid? _prior;
        private bool _disposed;
        public ScopePopper(Guid? prior) { _prior = prior; }
        public void Dispose()
        {
            if (_disposed) return;
            AmbientScope.Value = _prior;
            _disposed = true;
        }
    }
}
