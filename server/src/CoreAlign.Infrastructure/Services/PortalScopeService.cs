using System.Security.Claims;
using CoreAlign.Application.B2B;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CoreAlign.Infrastructure.Services;

/// <summary>
/// Resolves the active customer or dealer scope from the JWT-authenticated
/// caller's membership rows. The current user id is read once per call via
/// <see cref="IHttpContextAccessor"/> and never trusted from a route/query
/// parameter — this keeps every portal endpoint inherently scoped to the
/// caller regardless of which CustomerId the request body contains.
/// </summary>
public class PortalScopeService : IPortalScopeService
{
    private readonly ITenantContext _tenant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly IDealerUserRepository _dealerUsers;
    private readonly IDealerCustomerLinkRepository _links;

    public PortalScopeService(
        ITenantContext tenant,
        IHttpContextAccessor httpContextAccessor,
        ICustomerUserRepository customerUsers,
        IDealerUserRepository dealerUsers,
        IDealerCustomerLinkRepository links)
    {
        _tenant = tenant;
        _httpContextAccessor = httpContextAccessor;
        _customerUsers = customerUsers;
        _dealerUsers = dealerUsers;
        _links = links;
    }

    public async Task<Guid> GetCurrentCustomerIdAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var userId = ResolveCurrentUserId();

        var memberships = await _customerUsers.ListActiveByUserAsync(userId, tenantId, cancellationToken);
        var active = memberships.FirstOrDefault();
        if (active is null)
        {
            throw new PortalScopeNotResolvedException(
                "The current user has no active customer membership in this tenant.");
        }
        return active.CustomerId;
    }

    public async Task<Guid> GetCurrentDealerAccountIdAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var userId = ResolveCurrentUserId();

        var memberships = await _dealerUsers.ListActiveByUserAsync(userId, tenantId, cancellationToken);
        var active = memberships.FirstOrDefault();
        if (active is null)
        {
            throw new PortalScopeNotResolvedException(
                "The current user has no active dealer membership in this tenant.");
        }
        return active.DealerAccountId;
    }

    public async Task<IReadOnlyList<Guid>> GetDealerAllowedCustomerIdsAsync(CancellationToken cancellationToken = default)
    {
        var dealerAccountId = await GetCurrentDealerAccountIdAsync(cancellationToken);
        var links = await _links.ListByDealerAsync(dealerAccountId, cancellationToken);
        return links
            .Where(l => l.Status == DealerCustomerLinkStatus.Active)
            .Select(l => l.CustomerId)
            .Distinct()
            .ToList();
    }

    public async Task<Guid?> TryGetCurrentCustomerIdAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var userId = ResolveCurrentUserIdOrNull();
        if (userId is null)
        {
            return null;
        }
        var memberships = await _customerUsers.ListActiveByUserAsync(userId.Value, tenantId, cancellationToken);
        var active = memberships.FirstOrDefault();
        return active?.CustomerId;
    }

    public async Task<Guid?> TryGetCurrentDealerAccountIdAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.RequireTenantId();
        var userId = ResolveCurrentUserIdOrNull();
        if (userId is null)
        {
            return null;
        }
        var memberships = await _dealerUsers.ListActiveByUserAsync(userId.Value, tenantId, cancellationToken);
        var active = memberships.FirstOrDefault();
        return active?.DealerAccountId;
    }

    private Guid? ResolveCurrentUserIdOrNull()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null || ctx.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        var raw = ctx.User.FindFirstValue("sub")
                  ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var id) || id == Guid.Empty)
        {
            return null;
        }
        return id;
    }

    private Guid ResolveCurrentUserId()
    {
        var ctx = _httpContextAccessor.HttpContext
            ?? throw new PortalScopeNotResolvedException("No HTTP context for portal scope resolution.");

        if (ctx.User?.Identity?.IsAuthenticated != true)
        {
            throw new PortalScopeNotResolvedException("Caller is not authenticated.");
        }

        // The JWT handler is configured with DefaultMapInboundClaims = false, so
        // the user id comes through under "sub". Fall back to ClaimTypes.NameIdentifier
        // for tests that mint a ClaimsPrincipal directly.
        var raw = ctx.User.FindFirstValue("sub")
                  ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var id) || id == Guid.Empty)
        {
            throw new PortalScopeNotResolvedException("Current user id claim is missing or malformed.");
        }
        return id;
    }
}
