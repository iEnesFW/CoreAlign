using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B;

public class InviteDealerUserHandler : IRequestHandler<InviteDealerUserCommand, DealerUserDto>
{
    private readonly IDealerAccountRepository _dealers;
    private readonly IDealerUserRepository _dealerUsers;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public InviteDealerUserHandler(
        IDealerAccountRepository dealers,
        IDealerUserRepository dealerUsers,
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _dealers = dealers;
        _dealerUsers = dealerUsers;
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<DealerUserDto> Handle(InviteDealerUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var dealer = await _dealers.GetByIdAsync(request.DealerAccountId, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        _tenant.EnsureSameTenant(dealer.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageDealerAsync(request.CurrentUserId, callerRoles, dealer.Id, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot invite users for this dealer.");
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

        var existing = await _dealerUsers.GetByUserAndDealerAsync(user.Id, dealer.Id, cancellationToken);
        if (existing is not null)
        {
            throw new DuplicateDealerUserException();
        }

        var invitedBy = request.CurrentUserId == default ? (Guid?)null : request.CurrentUserId;
        var membership = new DealerUser(user.Id, dealer.Id, request.Role, invitedBy);
        await _dealerUsers.AddAsync(membership, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return B2BMappers.ToDto(membership, user, dealer);
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

public class UpdateDealerUserStatusHandler : IRequestHandler<UpdateDealerUserStatusCommand, DealerUserDto>
{
    private readonly IDealerUserRepository _dealerUsers;
    private readonly IDealerAccountRepository _dealers;
    private readonly IUserRepository _users;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public UpdateDealerUserStatusHandler(
        IDealerUserRepository dealerUsers,
        IDealerAccountRepository dealers,
        IUserRepository users,
        IB2BAuthorizationService authz,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _dealerUsers = dealerUsers;
        _dealers = dealers;
        _users = users;
        _authz = authz;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<DealerUserDto> Handle(UpdateDealerUserStatusCommand request, CancellationToken cancellationToken)
    {
        var membership = await _dealerUsers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new DealerUserNotFoundException();
        _tenant.EnsureSameTenant(membership.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageDealerAsync(request.CurrentUserId, callerRoles, membership.DealerAccountId, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot manage memberships for this dealer.");
        }

        switch (request.Status)
        {
            case MembershipStatus.Active: membership.Activate(); break;
            case MembershipStatus.Suspended: membership.Suspend(request.Reason); break;
            case MembershipStatus.Archived: membership.Archive(); break;
        }

        _dealerUsers.Update(membership);
        await _uow.SaveChangesAsync(cancellationToken);

        var user = await _users.GetByIdAsync(membership.UserId, cancellationToken)
            ?? throw new UserNotFoundException();
        var dealer = await _dealers.GetByIdAsync(membership.DealerAccountId, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        return B2BMappers.ToDto(membership, user, dealer);
    }
}

public class ListDealerUsersHandler : IRequestHandler<ListDealerUsersQuery, IReadOnlyList<DealerUserDto>>
{
    private readonly IDealerUserRepository _dealerUsers;
    private readonly IDealerAccountRepository _dealers;
    private readonly IUserRepository _users;
    private readonly IB2BAuthorizationService _authz;
    private readonly ITenantContext _tenant;

    public ListDealerUsersHandler(
        IDealerUserRepository dealerUsers,
        IDealerAccountRepository dealers,
        IUserRepository users,
        IB2BAuthorizationService authz,
        ITenantContext tenant)
    {
        _dealerUsers = dealerUsers;
        _dealers = dealers;
        _users = users;
        _authz = authz;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DealerUserDto>> Handle(ListDealerUsersQuery request, CancellationToken cancellationToken)
    {
        _tenant.RequireTenantId();
        var dealer = await _dealers.GetByIdAsync(request.DealerAccountId, cancellationToken)
            ?? throw new DealerAccountNotFoundException();
        _tenant.EnsureSameTenant(dealer.TenantId);

        var callerRoles = request.CurrentUserRoles ?? Array.Empty<string>();
        if (!await _authz.CanManageDealerAsync(request.CurrentUserId, callerRoles, dealer.Id, cancellationToken))
        {
            throw new B2BForbiddenException("Caller cannot list users for this dealer.");
        }

        var memberships = await _dealerUsers.ListByDealerAsync(dealer.Id, cancellationToken);
        if (memberships.Count == 0) return Array.Empty<DealerUserDto>();

        var result = new List<DealerUserDto>(memberships.Count);
        foreach (var m in memberships)
        {
            var u = await _users.GetByIdAsync(m.UserId, cancellationToken);
            if (u is null) continue;
            result.Add(B2BMappers.ToDto(m, u, dealer));
        }
        return result;
    }
}
