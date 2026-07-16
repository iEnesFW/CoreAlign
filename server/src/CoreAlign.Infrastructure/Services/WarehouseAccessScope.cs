using System.Security.Claims;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CoreAlign.Infrastructure.Services;

// WHY: opt-in accessor applied only to glass-plate reads — never a global StockItem query filter,
// which would break the warehouse-agnostic order-confirm / MRP / DRP / COGS paths.
public class WarehouseAccessScope : IWarehouseAccessScope
{
    private readonly ITenantContext _tenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserWarehouseAccessRepository _access;
    private readonly IConfiguration _configuration;

    public WarehouseAccessScope(
        ITenantContext tenant,
        IHttpContextAccessor httpContextAccessor,
        IUserWarehouseAccessRepository access,
        IConfiguration configuration)
    {
        _tenant = tenant;
        _httpContextAccessor = httpContextAccessor;
        _access = access;
        _configuration = configuration;
    }

    public async Task<WarehouseAccessResult> GetAllowedWarehouseIdsAsync(CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue<bool>("GlassPlateTracking:WarehouseAccessEnforced"))
        {
            return WarehouseAccessResult.Unrestricted;
        }

        var ctx = _httpContextAccessor.HttpContext;
        if (ctx?.User?.Identity?.IsAuthenticated != true)
        {
            return WarehouseAccessResult.Restricted(Array.Empty<Guid>());
        }

        if (IsAdmin(ctx.User))
        {
            return WarehouseAccessResult.Unrestricted;
        }

        var userId = ResolveCurrentUserId(ctx.User);
        if (userId is null)
        {
            return WarehouseAccessResult.Restricted(Array.Empty<Guid>());
        }

        var tenantId = _tenant.RequireTenantId();
        var allowed = await _access.GetWarehouseIdsByUserAsync(tenantId, userId.Value, cancellationToken);
        return WarehouseAccessResult.Restricted(allowed);
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(user.FindAll("role").Select(c => c.Value));
        return roles.Any(r => r is "TenantAdmin" or "PlatformAdmin");
    }

    private static Guid? ResolveCurrentUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }
}
