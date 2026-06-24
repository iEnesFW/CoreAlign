using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Auth.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILoginAuditLogRepository _loginAuditLogRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserMembershipService _userMembershipService;
    private readonly ITwoFactorChallengeRepository _twoFactorChallengeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginAuditLogRepository loginAuditLogRepository,
        IUserSessionRepository userSessionRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger)
        : this(userRepository, tenantRepository, refreshTokenRepository, loginAuditLogRepository,
               userSessionRepository, passwordHasher, jwtTokenService,
               null!, null!, unitOfWork, logger)
    {
    }

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginAuditLogRepository loginAuditLogRepository,
        IUserSessionRepository userSessionRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUserMembershipService userMembershipService,
        ITwoFactorChallengeRepository twoFactorChallengeRepository,
        IUnitOfWork unitOfWork,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _loginAuditLogRepository = loginAuditLogRepository;
        _userSessionRepository = userSessionRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _userMembershipService = userMembershipService;
        _twoFactorChallengeRepository = twoFactorChallengeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            await CommitFailedAttemptAsync(request.Email, LoginResultType.Failed, null, request, "User not found", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (user.IsLockedOut)
        {
            await CommitFailedAttemptAsync(request.Email, LoginResultType.Locked, user.Id, request, "Account locked", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Record the failed attempt against the user *and* the audit log in one
            // commit so lockout state and audit row stay consistent.
            user.RecordFailedLogin();
            _userRepository.Update(user);
            await CommitFailedAttemptAsync(request.Email, LoginResultType.Failed, user.Id, request, "Invalid password", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            await CommitFailedAttemptAsync(request.Email, LoginResultType.Disabled, user.Id, request, "Account disabled", cancellationToken);
            throw new AccountDisabledException();
        }

        if (!user.IsEmailConfirmed)
        {
            await CommitFailedAttemptAsync(request.Email, LoginResultType.Unverified, user.Id, request, "Email not verified", cancellationToken);
            throw new EmailNotVerifiedException();
        }

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        if (!tenant.IsActive || tenant.IsArchived)
        {
            await CommitFailedAttemptAsync(request.Email, LoginResultType.Disabled, user.Id, request, "Tenant inactive", cancellationToken);
            throw new TenantInactiveException();
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        if (!user.IsTwoFactorEnabled && !string.IsNullOrWhiteSpace(tenant.RequireTwoFactorForRoles))
        {
            var requiredRoles = tenant.RequireTwoFactorForRoles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (requiredRoles.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                throw new TwoFactorRequiredException();
            }
        }

        if (user.IsTwoFactorEnabled)
        {
            var challengeRaw = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var challengeHash = _jwtTokenService.HashToken(challengeRaw);
            var challenge = new TwoFactorChallenge(
                user.TenantId,
                user.Id,
                challengeHash,
                DateTime.UtcNow.AddMinutes(5),
                request.IpAddress,
                request.UserAgent);
            await _twoFactorChallengeRepository.AddAsync(challenge, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ExpiresAt = challenge.ExpiresAtUtc,
                RequiresTwoFactor = true,
                TwoFactorChallengeToken = challengeRaw,
                User = null
            };
        }

        user.RecordSuccessfulLogin();
        _userRepository.Update(user);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.TenantId, user.Email, roles);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashToken(rawRefreshToken);

        var refreshToken = new RefreshToken(
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(7),
            request.UserAgent,
            request.IpAddress);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        var session = new UserSession(
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(7),
            request.UserAgent,
            request.IpAddress);

        await _userSessionRepository.AddAsync(session, cancellationToken);

        // For successful login, attach the audit log to the same change-tracking
        // batch as the refresh token + session and commit them atomically. If the
        // commit fails the audit log is *not* persisted — preventing a "success"
        // audit row that has no matching session.
        var successLog = new LoginAuditLog(
            request.Email,
            LoginResultType.Success,
            user.Id,
            request.IpAddress,
            request.UserAgent,
            null);
        await _loginAuditLogRepository.AddAsync(successLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = MapToUserProfile(user, tenant, roles)
        };
    }

    /// <summary>
    /// Persist a failed-login audit row immediately, even though the handler is
    /// about to throw. Keeping this in its own commit means the throw doesn't
    /// roll back the audit trail, which is what we want for security forensics.
    /// </summary>
    private async Task CommitFailedAttemptAsync(
        string email,
        LoginResultType result,
        Guid? userId,
        LoginCommand request,
        string reason,
        CancellationToken ct)
    {
        // Structured security event — keep email *hashed-shape* (never the raw
        // plaintext) so log sinks like Datadog/Loki don't ingest PII for
        // unauthenticated attempts. UserId is fine when we already resolved the
        // account (post-lookup branches).
        _logger.LogWarning(
            "Login.{Result} reason={Reason} userId={UserId} ipPrefix={IpPrefix}",
            result,
            reason,
            userId,
            MaskIp(request.IpAddress));

        var log = new LoginAuditLog(email, result, userId, request.IpAddress, request.UserAgent, reason);
        await _loginAuditLogRepository.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Mask the host bits of the IP for log records (privacy-preserving).
    /// Keeps the /24 (IPv4) or /48 (IPv6) so geographic / abuse pattern remains
    /// useful without exposing exact subscriber identifiers.
    /// </summary>
    private static string MaskIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "-";
        if (ip.Contains(':'))
        {
            var parts = ip.Split(':');
            return parts.Length >= 3 ? $"{parts[0]}:{parts[1]}:{parts[2]}::/48" : ip;
        }
        var v4 = ip.Split('.');
        return v4.Length == 4 ? $"{v4[0]}.{v4[1]}.{v4[2]}.0/24" : ip;
    }

    private static UserProfileDto MapToUserProfile(User user, Tenant tenant, List<string> roles)
    {
        return new UserProfileDto
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
        };
    }
}
