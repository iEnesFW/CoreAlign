using CoreAlign.Application.Auth.Commands;
using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

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
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginAuditLogRepository loginAuditLogRepository,
        IUserSessionRepository userSessionRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _loginAuditLogRepository = loginAuditLogRepository;
        _userSessionRepository = userSessionRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            await LogLoginAttemptAsync(request.Email, LoginResultType.Failed, null, request.IpAddress, request.UserAgent, "User not found", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (user.IsLockedOut)
        {
            await LogLoginAttemptAsync(request.Email, LoginResultType.Locked, user.Id, request.IpAddress, request.UserAgent, "Account locked", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            _userRepository.Update(user);
            await LogLoginAttemptAsync(request.Email, LoginResultType.Failed, user.Id, request.IpAddress, request.UserAgent, "Invalid password", cancellationToken);
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            await LogLoginAttemptAsync(request.Email, LoginResultType.Disabled, user.Id, request.IpAddress, request.UserAgent, "Account disabled", cancellationToken);
            throw new AccountDisabledException();
        }

        if (!user.IsEmailConfirmed)
        {
            await LogLoginAttemptAsync(request.Email, LoginResultType.Unverified, user.Id, request.IpAddress, request.UserAgent, "Email not verified", cancellationToken);
            throw new EmailNotVerifiedException();
        }

        user.RecordSuccessfulLogin();
        _userRepository.Update(user);

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken)
            ?? throw new UserNotFoundException();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
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
        await LogLoginAttemptAsync(request.Email, LoginResultType.Success, user.Id, request.IpAddress, request.UserAgent, null, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = MapToUserProfile(user, tenant, roles)
        };
    }

    private async Task LogLoginAttemptAsync(string email, LoginResultType result, Guid? userId, string? ip, string? ua, string? reason, CancellationToken ct)
    {
        var log = new LoginAuditLog(email, result, userId, ip, ua, reason);
        await _loginAuditLogRepository.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);
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
            Roles = roles
        };
    }
}
