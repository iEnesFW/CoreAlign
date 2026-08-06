using MediatR;

namespace CoreAlign.Application.Notifications.Smtp;

public sealed record TenantSmtpSettingsDto(
    bool IsConfigured,
    bool IsEnabled,
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? FromAddress,
    string? FromName,
    bool HasPassword,
    string? LastHealthStatus,
    DateTime? LastHealthCheckUtc,
    string AuthMode = SmtpAuthModes.Password,
    string? OAuthProvider = null,
    string? OAuthTenantId = null,
    string? OAuthClientId = null,
    string? OAuthTokenEndpoint = null,
    string? OAuthScope = null,
    bool HasOAuthClientSecret = false,
    bool HasOAuthRefreshToken = false);

public sealed record UpsertTenantSmtpSettingsCommand(
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string? FromAddress,
    string? FromName,
    bool IsEnabled,
    string? AuthMode = null,
    string? OAuthProvider = null,
    string? OAuthTenantId = null,
    string? OAuthClientId = null,
    string? OAuthClientSecret = null,
    string? OAuthRefreshToken = null,
    string? OAuthTokenEndpoint = null,
    string? OAuthScope = null) : IRequest<TenantSmtpSettingsDto>;

public sealed record GetTenantSmtpSettingsQuery : IRequest<TenantSmtpSettingsDto>;

public sealed record SendTestEmailCommand(string ToAddress) : IRequest<SendTestEmailResult>;

public sealed record SendTestEmailResult(bool Success, string? Message);

public sealed record SmtpHealthResult(bool IsHealthy, string? Message, DateTime CheckedAtUtc);

public sealed record CheckSmtpHealthQuery : IRequest<SmtpHealthResult>;
