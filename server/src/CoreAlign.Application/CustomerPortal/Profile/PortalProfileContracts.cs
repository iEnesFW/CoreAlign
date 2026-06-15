using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.CustomerPortal.Profile;

public record GetPortalProfileQuery() : IRequest<PortalProfileDto>;

public record UpdatePortalProfileCommand(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? PreferredLocale) : IRequest<PortalProfileDto>, ITransactionalRequest;

public record PortalProfileDto(
    Guid UserId,
    string Email,
    string Username,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? PreferredLocale,
    bool IsTwoFactorEnabled,
    Guid TenantId,
    string TenantName);

public record ListPortalSessionsQuery() : IRequest<IReadOnlyList<PortalSessionDto>>;

public record RevokeAllPortalSessionsCommand() : IRequest<int>, ITransactionalRequest;

public record PortalSessionDto(
    Guid Id,
    string? DeviceInfo,
    string? IpAddress,
    DateTime CreatedAtUtc,
    DateTime LastActivityAtUtc,
    DateTime ExpiresAtUtc,
    bool IsCurrent);
