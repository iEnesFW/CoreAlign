using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Users;

internal static class UserMapper
{
    public static UserSummaryDto ToDto(User u) => new(
        u.Id,
        u.Username,
        u.Email,
        u.FirstName,
        u.LastName,
        u.IsActive,
        u.IsEmailConfirmed,
        u.UserRoles.Select(ur => ur.RoleId).ToList(),
        u.UserRoles.Select(ur => ur.Role).Where(r => r is not null).Select(r => r!.Name).ToList(),
        u.LastLoginAtUtc,
        u.CreatedAtUtc);

    public static RoleDto ToDto(Role r) => new(r.Id, r.Name, r.Description);
}

public class ListUsersHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<UserSummaryDto>>
{
    private readonly IUserRepository _users;
    private readonly ITenantContext _tenant;
    public ListUsersHandler(IUserRepository users, ITenantContext tenant) { _users = users; _tenant = tenant; }

    public async Task<IReadOnlyList<UserSummaryDto>> Handle(ListUsersQuery q, CancellationToken ct)
    {
        var users = await _users.ListByTenantAsync(_tenant.RequireTenantId(), ct);
        return users.Select(UserMapper.ToDto).ToList();
    }
}

public class ListRolesHandler : IRequestHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleRepository _roles;
    public ListRolesHandler(IRoleRepository roles) => _roles = roles;

    public async Task<IReadOnlyList<RoleDto>> Handle(ListRolesQuery q, CancellationToken ct)
        => (await _roles.ListAsync(ct)).Select(UserMapper.ToDto).ToList();
}

public class InviteUserHandler : IRequestHandler<InviteUserCommand, UserSummaryDto>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public InviteUserHandler(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<UserSummaryDto> Handle(InviteUserCommand c, CancellationToken ct)
    {
        var tenantId = _tenant.RequireTenantId();
        if (await _users.ExistsByUsernameAsync(c.Username, ct)) throw new DuplicateUsernameException();
        if (await _users.ExistsByEmailAsync(c.Email, ct)) throw new DuplicateEmailException();

        var user = new User(tenantId, c.Username.Trim(), c.Email.Trim(), _passwordHasher.Hash(c.Password))
        {
            FirstName = string.IsNullOrWhiteSpace(c.FirstName) ? null : c.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(c.LastName) ? null : c.LastName.Trim(),
            IsEmailConfirmed = true,
            IsActive = true,
        };

        var roleIds = c.RoleIds.Count > 0
            ? (await _roles.GetByIdsAsync(c.RoleIds, ct)).Select(r => r.Id).ToList()
            : new List<int>();
        if (roleIds.Count == 0)
        {
            var defaultRole = await _roles.GetByNameAsync("User", ct);
            if (defaultRole is not null) roleIds.Add(defaultRole.Id);
        }
        var assignedBy = c.InvitedByUserId == default ? (Guid?)null : c.InvitedByUserId;
        foreach (var roleId in roleIds)
        {
            user.UserRoles.Add(new UserRole(user.Id, roleId) { AssignedByUserId = assignedBy });
        }

        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        var saved = await _users.GetByIdAsync(user.Id, ct);
        return UserMapper.ToDto(saved ?? user);
    }
}

public class UpdateUserRolesHandler : IRequestHandler<UpdateUserRolesCommand, UserSummaryDto>
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public UpdateUserRolesHandler(IUserRepository users, IRoleRepository roles, ITenantContext tenant, IUnitOfWork uow)
    {
        _users = users;
        _roles = roles;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<UserSummaryDto> Handle(UpdateUserRolesCommand c, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(c.UserId, ct) ?? throw new UserNotFoundException();
        _tenant.EnsureSameTenant(user.TenantId);

        var validRoleIds = (await _roles.GetByIdsAsync(c.RoleIds, ct)).Select(r => r.Id).ToHashSet();
        user.UserRoles.Clear();
        foreach (var roleId in validRoleIds)
        {
            user.UserRoles.Add(new UserRole(user.Id, roleId));
        }
        user.UpdatedAtUtc = DateTime.UtcNow;
        _users.Update(user);
        await _uow.SaveChangesAsync(ct);

        var saved = await _users.GetByIdAsync(user.Id, ct);
        return UserMapper.ToDto(saved ?? user);
    }
}

public class SetUserActiveHandler : IRequestHandler<SetUserActiveCommand, UserSummaryDto>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserSessionRepository _sessions;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;

    public SetUserActiveHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUserSessionRepository sessions,
        ITenantContext tenant,
        IUnitOfWork uow)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _sessions = sessions;
        _tenant = tenant;
        _uow = uow;
    }

    public async Task<UserSummaryDto> Handle(SetUserActiveCommand c, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(c.UserId, ct) ?? throw new UserNotFoundException();
        _tenant.EnsureSameTenant(user.TenantId);
        if (!c.IsActive && user.Id == c.CurrentUserId) throw new CannotDeactivateSelfException();

        user.IsActive = c.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        if (!c.IsActive)
        {
            user.ResetSecurityStamp();
            await _refreshTokens.RevokeAllByUserIdAsync(user.Id, ct);
            await _sessions.RevokeAllByUserIdAsync(user.Id, ct);
        }
        _users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return UserMapper.ToDto(user);
    }
}
