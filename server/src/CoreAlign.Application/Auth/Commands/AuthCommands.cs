using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<AuthResponseDto>;

public record RegisterCommand(
    string OrganizationName,
    string Username,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null
) : IRequest<AuthResponseDto>, ITransactionalRequest;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<AuthResponseDto>, ITransactionalRequest;

public record ForgotPasswordCommand(
    string Email
) : IRequest<bool>, ITransactionalRequest;

public record ResetPasswordCommand(
    string Token,
    string NewPassword
) : IRequest<bool>, ITransactionalRequest;

public record VerifyEmailCommand(
    string Token
) : IRequest<bool>, ITransactionalRequest;

public record LogoutCommand(
    string RefreshToken
) : IRequest<bool>, ITransactionalRequest;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<bool>, ITransactionalRequest;

public record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? AvatarUrl
) : IRequest<AuthResponseDto>, ITransactionalRequest;
