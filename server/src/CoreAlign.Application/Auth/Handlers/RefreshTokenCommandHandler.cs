using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly ILoginAuditLogRepository _loginAuditLogRepository;
    private readonly ISecurityAlertOutbox _securityAlertOutbox;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserMembershipService _userMembershipService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ITenantRepository tenantRepository,
        IUserSessionRepository userSessionRepository,
        ILoginAuditLogRepository loginAuditLogRepository,
        ISecurityAlertOutbox securityAlertOutbox,
        IJwtTokenService jwtTokenService,
        IUserMembershipService userMembershipService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tenantRepository = tenantRepository;
        _userSessionRepository = userSessionRepository;
        _loginAuditLogRepository = loginAuditLogRepository;
        _securityAlertOutbox = securityAlertOutbox;
        _jwtTokenService = jwtTokenService;
        _userMembershipService = userMembershipService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new TokenExpiredException();
        }

        if (existingToken.IsRevoked)
        {
            var chain = await _refreshTokenRepository.ListChainFromAsync(tokenHash, cancellationToken);
            var activeDescendantIds = chain
                .Where(t => t.IsActive)
                .Select(t => t.Id)
                .ToList();

            if (activeDescendantIds.Count > 0)
            {
                await _refreshTokenRepository.RevokeManyAsync(activeDescendantIds, cancellationToken);
            }

            var userId = existingToken.UserId;
            await _userSessionRepository.RevokeAllByUserIdAsync(userId, cancellationToken);

            var reuseLog = new LoginAuditLog(
                existingToken.User?.Email ?? string.Empty,
                LoginResultType.Failed,
                userId,
                request.IpAddress,
                request.DeviceInfo,
                "RefreshTokenReuse");
            await _loginAuditLogRepository.AddAsync(reuseLog, cancellationToken);

            await _securityAlertOutbox.EnqueueRefreshTokenReuseAsync(
                userId,
                DateTime.UtcNow,
                request.IpAddress,
                request.DeviceInfo,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new TokenExpiredException();
        }

        if (!existingToken.IsActive)
        {
            throw new TokenExpiredException();
        }

        var user = existingToken.User
            ?? throw new UserNotFoundException();
        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (!user.IsActive)
        {
            throw new AccountDisabledException();
        }
        if (!tenant.IsActive || tenant.IsArchived)
        {
            throw new TenantInactiveException();
        }

        var newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashToken(newRawRefreshToken);

        existingToken.Revoke(newRefreshTokenHash);
        _refreshTokenRepository.Update(existingToken);

        var newRefreshToken = new RefreshToken(
            user.Id,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(7),
            request.DeviceInfo,
            request.IpAddress);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var persona = await _userMembershipService.ResolvePersonaAsync(user.Id, user.TenantId, cancellationToken);
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.TenantId,
            user.Email,
            roles,
            persona.ToString().ToLowerInvariant());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserProfileDto
            {
                Id = user.Id,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                TenantSlug = tenant.Slug,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl,
                Roles = roles,
                PreferredLocale = user.PreferredLocale
            }
        };
    }
}
