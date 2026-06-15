using CoreAlign.Application.B2B;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Profile;

public class GetPortalProfileHandler : IRequestHandler<GetPortalProfileQuery, PortalProfileDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;

    public GetPortalProfileHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserRepository users,
        ITenantRepository tenants)
    {
        _scope = scope;
        _currentUser = currentUser;
        _users = users;
        _tenants = tenants;
    }

    public async Task<PortalProfileDto> Handle(GetPortalProfileQuery request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();

        var user = await _users.GetByIdAsync(userId, cancellationToken) ?? throw new UserNotFoundException();
        var tenant = await _tenants.GetByIdAsync(user.TenantId, cancellationToken) ?? throw new UserNotFoundException();

        return new PortalProfileDto(
            user.Id,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.PreferredLocale,
            user.IsTwoFactorEnabled,
            tenant.Id,
            tenant.Name);
    }
}

public class UpdatePortalProfileHandler : IRequestHandler<UpdatePortalProfileCommand, PortalProfileDto>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserRepository _users;
    private readonly ITenantRepository _tenants;
    private readonly IUnitOfWork _uow;

    public UpdatePortalProfileHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserRepository users,
        ITenantRepository tenants,
        IUnitOfWork uow)
    {
        _scope = scope;
        _currentUser = currentUser;
        _users = users;
        _tenants = tenants;
        _uow = uow;
    }

    public async Task<PortalProfileDto> Handle(UpdatePortalProfileCommand request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();

        var user = await _users.GetByIdAsync(userId, cancellationToken) ?? throw new UserNotFoundException();
        var tenant = await _tenants.GetByIdAsync(user.TenantId, cancellationToken) ?? throw new UserNotFoundException();

        user.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim();
        user.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.PreferredLocale = NormalizeLocale(request.PreferredLocale);
        user.UpdatedAtUtc = DateTime.UtcNow;
        _users.Update(user);

        await _uow.SaveChangesAsync(cancellationToken);

        return new PortalProfileDto(
            user.Id,
            user.Email,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.PreferredLocale,
            user.IsTwoFactorEnabled,
            tenant.Id,
            tenant.Name);
    }

    private static string? NormalizeLocale(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim().ToLowerInvariant();
        return trimmed is "tr" or "en" or "tr-tr" or "en-us" ? trimmed : null;
    }
}

public class ListPortalSessionsHandler : IRequestHandler<ListPortalSessionsQuery, IReadOnlyList<PortalSessionDto>>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserSessionRepository _sessions;

    public ListPortalSessionsHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserSessionRepository sessions)
    {
        _scope = scope;
        _currentUser = currentUser;
        _sessions = sessions;
    }

    public async Task<IReadOnlyList<PortalSessionDto>> Handle(ListPortalSessionsQuery request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();

        var rows = await _sessions.GetActiveByUserIdAsync(userId, cancellationToken);
        return rows
            .OrderByDescending(s => s.LastActivityAtUtc)
            .Select(s => new PortalSessionDto(
                s.Id,
                s.DeviceInfo,
                s.IpAddress,
                s.CreatedAtUtc,
                s.LastActivityAtUtc,
                s.ExpiresAtUtc,
                IsCurrent: false))
            .ToList();
    }
}

public class RevokeAllPortalSessionsHandler : IRequestHandler<RevokeAllPortalSessionsCommand, int>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IUserSessionRepository _sessions;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _uow;

    public RevokeAllPortalSessionsHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IUserSessionRepository sessions,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork uow)
    {
        _scope = scope;
        _currentUser = currentUser;
        _sessions = sessions;
        _refreshTokens = refreshTokens;
        _uow = uow;
    }

    public async Task<int> Handle(RevokeAllPortalSessionsCommand request, CancellationToken cancellationToken)
    {
        await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();

        var active = await _sessions.GetActiveByUserIdAsync(userId, cancellationToken);
        await _sessions.RevokeAllByUserIdAsync(userId, cancellationToken);
        await _refreshTokens.RevokeAllByUserIdAsync(userId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return active.Count;
    }
}
