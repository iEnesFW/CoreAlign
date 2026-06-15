using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Users;

public record RoleDto(int Id, string Name, string? Description);

public record UserSummaryDto(
    Guid Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    bool IsEmailConfirmed,
    IReadOnlyList<int> RoleIds,
    IReadOnlyList<string> Roles,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc);

public record ListUsersQuery : IRequest<IReadOnlyList<UserSummaryDto>>;

public record ListRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public record InviteUserCommand(
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string Password,
    List<int> RoleIds,
    Guid InvitedByUserId = default) : IRequest<UserSummaryDto>, ITransactionalRequest;

public record UpdateUserRolesCommand(
    Guid UserId,
    List<int> RoleIds) : IRequest<UserSummaryDto>, ITransactionalRequest;

public record SetUserActiveCommand(
    Guid UserId,
    bool IsActive,
    Guid CurrentUserId = default) : IRequest<UserSummaryDto>, ITransactionalRequest;
