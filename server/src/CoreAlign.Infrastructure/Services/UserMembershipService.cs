using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Services;

public class UserMembershipService : IUserMembershipService
{
    private readonly IDealerUserRepository _dealerUsers;
    private readonly ICustomerUserRepository _customerUsers;

    public UserMembershipService(IDealerUserRepository dealerUsers, ICustomerUserRepository customerUsers)
    {
        _dealerUsers = dealerUsers;
        _customerUsers = customerUsers;
    }

    public async Task<UserPersona> ResolvePersonaAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (await _dealerUsers.AnyActiveForUserAsync(userId, tenantId, cancellationToken))
        {
            return UserPersona.Dealer;
        }
        if (await _customerUsers.AnyActiveForUserAsync(userId, tenantId, cancellationToken))
        {
            return UserPersona.Customer;
        }
        return UserPersona.Tenant;
    }
}

public class B2BAuthorizationService : IB2BAuthorizationService
{
    public const string TenantAdminRole = "TenantAdmin";

    private readonly ICustomerUserRepository _customerUsers;
    private readonly IDealerUserRepository _dealerUsers;
    private readonly IDealerCustomerLinkRepository _links;

    public B2BAuthorizationService(
        ICustomerUserRepository customerUsers,
        IDealerUserRepository dealerUsers,
        IDealerCustomerLinkRepository links)
    {
        _customerUsers = customerUsers;
        _dealerUsers = dealerUsers;
        _links = links;
    }

    public Task<bool> IsCustomerOwnerAsync(Guid userId, Guid customerId, CancellationToken cancellationToken = default) =>
        _customerUsers.HasActiveOwnershipAsync(userId, customerId, cancellationToken);

    public Task<bool> IsDealerOwnerAsync(Guid userId, Guid dealerAccountId, CancellationToken cancellationToken = default) =>
        _dealerUsers.HasActiveOwnershipAsync(userId, dealerAccountId, cancellationToken);

    public async Task<bool> CanManageCustomerAsync(Guid userId, IReadOnlyCollection<string> roles, Guid customerId, CancellationToken cancellationToken = default)
    {
        if (roles.Contains(TenantAdminRole)) return true;
        return await IsCustomerOwnerAsync(userId, customerId, cancellationToken);
    }

    public async Task<bool> CanManageDealerAsync(Guid userId, IReadOnlyCollection<string> roles, Guid dealerAccountId, CancellationToken cancellationToken = default)
    {
        if (roles.Contains(TenantAdminRole)) return true;
        if (await IsDealerOwnerAsync(userId, dealerAccountId, cancellationToken)) return true;

        var links = await _links.ListByDealerAsync(dealerAccountId, cancellationToken);
        foreach (var link in links)
        {
            if (link.Status != DealerCustomerLinkStatus.Active) continue;
            if (await _customerUsers.HasActiveOwnershipAsync(userId, link.CustomerId, cancellationToken)) return true;
        }
        return false;
    }
}
