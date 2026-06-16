using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B;

public class InviteCustomerUserHandler : IRequestHandler<InviteCustomerUserCommand, CustomerUserDto>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerUserRepository _customerUsers;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public InviteCustomerUserHandler(
        ICustomerRepository customers,
        ICustomerUserRepository customerUsers,
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _customers = customers;
        _customerUsers = customerUsers;
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<CustomerUserDto> Handle(InviteCustomerUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        _tenant.EnsureSameTenant(customer.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageCustomerAsync(request.CurrentUserId, callerRoles, customer.Id, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot invite users for this customer.");
        }

        var email = request.Email.Trim();
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            var username = await BuildUniqueUsernameAsync(email, cancellationToken);
            user = new User(tenantId, username, email, _passwordHasher.Hash(Guid.NewGuid().ToString("N")))
            {
                FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
                IsActive = true,
                IsEmailConfirmed = false,
            };
            var defaultRole = await _roles.GetByNameAsync("User", cancellationToken);
            if (defaultRole is not null)
            {
                user.UserRoles.Add(new UserRole(user.Id, defaultRole.Id));
            }
            await _users.AddAsync(user, cancellationToken);
        }
        else
        {
            _tenant.EnsureSameTenant(user.TenantId);
        }

        var existingMembership = await _customerUsers.GetByUserAndCustomerAsync(user.Id, customer.Id, cancellationToken);
        if (existingMembership is not null)
        {
            throw new DuplicateCustomerUserException();
        }

        var invitedBy = request.CurrentUserId == default ? (Guid?)null : request.CurrentUserId;
        var membership = new CustomerUser(user.Id, customer.Id, request.Role, invitedBy);
        await _customerUsers.AddAsync(membership, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return B2BMappers.ToDto(membership, user, customer);
    }

    private async Task<string> BuildUniqueUsernameAsync(string email, CancellationToken ct)
    {
        var baseName = email.Split('@')[0];
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "user";
        var candidate = baseName;
        var suffix = 0;
        while (await _users.ExistsByUsernameAsync(candidate, ct))
        {
            suffix++;
            candidate = $"{baseName}{suffix}";
        }
        return candidate;
    }
}

public class UpdateCustomerUserStatusHandler : IRequestHandler<UpdateCustomerUserStatusCommand, CustomerUserDto>
{
    private readonly ICustomerUserRepository _customerUsers;
    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public UpdateCustomerUserStatusHandler(
        ICustomerUserRepository customerUsers,
        ICustomerRepository customers,
        IUserRepository users,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _customerUsers = customerUsers;
        _customers = customers;
        _users = users;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<CustomerUserDto> Handle(UpdateCustomerUserStatusCommand request, CancellationToken cancellationToken)
    {
        var membership = await _customerUsers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new CustomerUserNotFoundException();
        _tenant.EnsureSameTenant(membership.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageCustomerAsync(request.CurrentUserId, callerRoles, membership.CustomerId, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot manage memberships for this customer.");
        }

        switch (request.Status)
        {
            case MembershipStatus.Active: membership.Activate(); break;
            case MembershipStatus.Suspended: membership.Suspend(request.Reason); break;
            case MembershipStatus.Archived: membership.Archive(); break;
        }

        _customerUsers.Update(membership);
        await _uow.SaveChangesAsync(cancellationToken);

        var user = await _users.GetByIdAsync(membership.UserId, cancellationToken)
            ?? throw new UserNotFoundException();
        var customer = await _customers.GetByIdAsync(membership.CustomerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
        return B2BMappers.ToDto(membership, user, customer);
    }
}

public class ListCustomerUsersHandler : IRequestHandler<ListCustomerUsersQuery, IReadOnlyList<CustomerUserDto>>
{
    private readonly ICustomerUserRepository _customerUsers;
    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;

    public ListCustomerUsersHandler(
        ICustomerUserRepository customerUsers,
        ICustomerRepository customers,
        IUserRepository users,
        IB2BAuthorizationService authz,
        ITenantContext tenant)
    {
        _customerUsers = customerUsers;
        _customers = customers;
        _users = users;
        _authz = authz;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<CustomerUserDto>> Handle(ListCustomerUsersQuery request, CancellationToken cancellationToken)
    {
        _tenant.RequireTenantId();
        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();

        IReadOnlyList<CustomerUser> memberships;
        if (request.CustomerId.HasValue)
        {
            if (!await _authz.CanManageCustomerAsync(request.CurrentUserId, callerRoles, request.CustomerId.Value, cancellationToken))
            {
                throw new B2BForbiddenException("Caller cannot list users for this customer.");
            }
            memberships = await _customerUsers.ListByCustomerAsync(request.CustomerId.Value, cancellationToken);
        }
        else
        {
            if (callerRoles.Contains(B2BAuthorizationRoles.TenantAdmin))
            {
                memberships = await _customerUsers.ListByTenantAsync(cancellationToken);
            }
            else
            {
                var owned = await _customerUsers.ListActiveByUserAsync(request.CurrentUserId, _tenant.RequireTenantId(), cancellationToken);
                var ownedCustomerIds = owned
                    .Where(m => m.MembershipRole == CustomerMembershipRole.CustomerOwner)
                    .Select(m => m.CustomerId)
                    .ToHashSet();
                if (ownedCustomerIds.Count == 0) return Array.Empty<CustomerUserDto>();
                var all = await _customerUsers.ListByTenantAsync(cancellationToken);
                memberships = all.Where(m => ownedCustomerIds.Contains(m.CustomerId)).ToList();
            }
        }

        if (memberships.Count == 0) return Array.Empty<CustomerUserDto>();

        // Batch-load users and customers in one query each (was 2 sequential GetByIdAsync
        // per membership over an unpaginated tenant-wide list — 1+2N round-trips).
        var userMap = (await _users.ListByIdsAsync(memberships.Select(m => m.UserId).Distinct(), cancellationToken))
            .ToDictionary(u => u.Id);
        var customerMap = await _customers.GetByIdsAsync(memberships.Select(m => m.CustomerId).Distinct(), cancellationToken);

        var result = new List<CustomerUserDto>(memberships.Count);
        foreach (var m in memberships)
        {
            if (!userMap.TryGetValue(m.UserId, out var u) || !customerMap.TryGetValue(m.CustomerId, out var c)) continue;
            result.Add(B2BMappers.ToDto(m, u, c));
        }
        return result;
    }
}

internal static class B2BAuthorizationRoles
{
    public const string TenantAdmin = "TenantAdmin";
}
