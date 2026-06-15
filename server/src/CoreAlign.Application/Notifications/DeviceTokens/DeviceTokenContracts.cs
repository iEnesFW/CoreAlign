using MediatR;

namespace CoreAlign.Application.Notifications.DeviceTokens;

public sealed record RegisterDeviceTokenRequest(
    string Token,
    string Platform,
    string? DeviceName,
    string? OsVersion);

public sealed record DeviceTokenDto(
    Guid Id,
    string Platform,
    string? DeviceName,
    string? OsVersion,
    DateTime LastSeenAtUtc,
    DateTime CreatedAtUtc);

public sealed record RegisterDeviceTokenCommand(
    string Token,
    string Platform,
    string? DeviceName,
    string? OsVersion) : IRequest<DeviceTokenDto>;

public sealed record DeactivateDeviceTokenCommand(string Token) : IRequest<bool>;
