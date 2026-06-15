using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public class GetDealerPortalProfileHandler : IRequestHandler<GetDealerPortalProfileQuery, DealerPortalProfileDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly IDealerAccountRepository _dealers;
    private readonly IDealerUserRepository _dealerUsers;
    private readonly ITenantContext _tenant;

    public GetDealerPortalProfileHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserRepository users,
        ITenantRepository tenants,
        IDealerAccountRepository dealers,
        IDealerUserRepository dealerUsers,
        ITenantContext tenant)
    {
        _scope = scope;
        _currentUser = currentUser;
        _users = users;
        _tenants = tenants;
        _dealers = dealers;
        _dealerUsers = dealerUsers;
        _tenant = tenant;
    }

    public async Task<DealerPortalProfileDto> Handle(GetDealerPortalProfileQuery request, CancellationToken cancellationToken)
    {
        var dealerAccountId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();
        var tenantId = _tenant.RequireTenantId();

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new PortalScopeNotResolvedException("Current user not found.");
        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new MissingTenantContextException();
        var dealer = await _dealers.GetByIdAsync(dealerAccountId, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        var membership = await _dealerUsers.GetByUserAndDealerAsync(userId, dealerAccountId, cancellationToken)
            ?? throw new DealerUserNotFoundException();

        return new DealerPortalProfileDto(
            UserId: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            TenantName: tenant.Name,
            DealerAccountId: dealer.Id,
            DealerName: dealer.Name,
            DealerCode: dealer.Code,
            MembershipRole: membership.MembershipRole,
            LastLoginAtUtc: user.LastLoginAtUtc);
    }
}
