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
    DateTime? LastHealthCheckUtc);

public sealed record UpsertTenantSmtpSettingsCommand(
    string Host,
    int Port,
    bool UseSsl,
    string? Username,
    string? Password,
    string? FromAddress,
    string? FromName,
    bool IsEnabled) : IRequest<TenantSmtpSettingsDto>;

public sealed record GetTenantSmtpSettingsQuery : IRequest<TenantSmtpSettingsDto>;

public sealed record SendTestEmailCommand(string ToAddress) : IRequest<SendTestEmailResult>;

public sealed record SendTestEmailResult(bool Success, string? Message);

public sealed record SmtpHealthResult(bool IsHealthy, string? Message, DateTime CheckedAtUtc);

public sealed record CheckSmtpHealthQuery : IRequest<SmtpHealthResult>;
