using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Auth.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Auth.Handlers;

public class CompleteTwoFactorChallengeCommandHandler : IRequestHandler<CompleteTwoFactorChallengeCommand, AuthResponseDto>
{
    private readonly ITwoFactorChallengeRepository _challengeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ITwoFactorBackupCodeRepository _backupCodeRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILoginAuditLogRepository _loginAuditLogRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserMembershipService _userMembershipService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteTwoFactorChallengeCommandHandler(
        ITwoFactorChallengeRepository challengeRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITwoFactorService twoFactorService,
        ITwoFactorBackupCodeRepository backupCodeRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginAuditLogRepository loginAuditLogRepository,
        IUserSessionRepository userSessionRepository,
        IJwtTokenService jwtTokenService,
        IUserMembershipService userMembershipService,
        IUnitOfWork unitOfWork)
    {
        _challengeRepository = challengeRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _twoFactorService = twoFactorService;
        _backupCodeRepository = backupCodeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _loginAuditLogRepository = loginAuditLogRepository;
        _userSessionRepository = userSessionRepository;
        _jwtTokenService = jwtTokenService;
        _userMembershipService = userMembershipService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(CompleteTwoFactorChallengeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeToken))
        {
            throw new InvalidTwoFactorChallengeException();
        }

        if (string.IsNullOrWhiteSpace(request.Code) && string.IsNullOrWhiteSpace(request.BackupCode))
        {
            throw new InvalidTwoFactorCodeException();
        }

        var challengeHash = _jwtTokenService.HashToken(request.ChallengeToken);
        var challenge = await _challengeRepository.FindByTokenHashAsync(challengeHash, cancellationToken);
        if (challenge is null || challenge.IsConsumed || challenge.IsExpired(DateTime.UtcNow))
        {
            throw new InvalidTwoFactorChallengeException();
        }

        var user = challenge.User ?? await _userRepository.GetByIdAsync(challenge.UserId, cancellationToken);
        if (user is null || !user.IsTwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecretKey))
        {
            throw new InvalidTwoFactorChallengeException();
        }

        var verified = await VerifyAsync(user, request, cancellationToken);
        if (!verified)
        {
            var failedLog = new LoginAuditLog(
                user.Email,
                LoginResultType.TwoFactorFailed,
                user.Id,
                request.IpAddress,
                request.UserAgent,
                "Invalid 2FA code or backup code");
            await _loginAuditLogRepository.AddAsync(failedLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidTwoFactorCodeException();
        }

        challenge.Consume(DateTime.UtcNow);
        _challengeRepository.Update(challenge);

        user.RecordSuccessfulLogin();
        _userRepository.Update(user);

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var persona = await _userMembershipService.ResolvePersonaAsync(user.Id, user.TenantId, cancellationToken);
        var personaString = PersonaToString(persona);
        var mfaVerifiedAt = DateTime.UtcNow;
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.TenantId,
            user.Email,
            roles,
            personaString,
            mfaVerifiedAt);
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

        var successLog = new LoginAuditLog(
            user.Email,
            LoginResultType.TwoFactorSuccess,
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
            User = MapToUserProfile(user, tenant, roles, personaString),
            RequiresTwoFactor = false,
        };
    }

    private async Task<bool> VerifyAsync(User user, CompleteTwoFactorChallengeCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            return _twoFactorService.Verify(user.TwoFactorSecretKey!, request.Code);
        }

        var hash = _twoFactorService.HashBackupCode(request.BackupCode!);
        var backup = await _backupCodeRepository.FindActiveByHashAsync(user.Id, hash, cancellationToken);
        if (backup is null) return false;

        backup.MarkUsed(DateTime.UtcNow);
        _backupCodeRepository.Update(backup);
        return true;
    }

    private static string PersonaToString(UserPersona persona) => persona switch
    {
        UserPersona.Dealer => "dealer",
        UserPersona.Customer => "customer",
        _ => "tenant",
    };

    private static UserProfileDto MapToUserProfile(User user, Tenant tenant, List<string> roles, string persona)
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
            Persona = persona,
            PreferredLocale = user.PreferredLocale,
        };
    }
}
