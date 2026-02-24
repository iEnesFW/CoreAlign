using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<ApiResponse<AuthResponseDto>>;

public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null
) : IRequest<ApiResponse<AuthResponseDto>>;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? DeviceInfo = null
) : IRequest<ApiResponse<AuthResponseDto>>;

public record ForgotPasswordCommand(
    string Email
) : IRequest<ApiResponse<bool>>;

public record ResetPasswordCommand(
    string Token,
    string NewPassword
) : IRequest<ApiResponse<bool>>;

public record VerifyEmailCommand(
    string Token
) : IRequest<ApiResponse<bool>>;

public record LogoutCommand(
    string RefreshToken
) : IRequest<ApiResponse<bool>>;
