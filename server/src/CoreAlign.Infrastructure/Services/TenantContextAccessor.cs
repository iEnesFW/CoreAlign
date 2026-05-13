using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CoreAlign.Infrastructure.Services;

public class TenantContextAccessor : ITenantContext
{
    public const string TenantClaimType = "tenant_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentTenantId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirst(TenantClaimType)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool HasTenant => CurrentTenantId.HasValue;
}
