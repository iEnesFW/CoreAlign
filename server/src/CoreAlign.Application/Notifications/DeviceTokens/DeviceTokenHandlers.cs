using CoreAlign.Application.B2B;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Notifications.DeviceTokens;

public sealed class RegisterDeviceTokenHandler : IRequestHandler<RegisterDeviceTokenCommand, DeviceTokenDto>
{
    private static readonly HashSet<string> AllowedPlatforms = new(StringComparer.OrdinalIgnoreCase)
    {
        "ios", "android", "web",
    };

    private readonly IUserDeviceTokenRepository _repository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _uow;

    public RegisterDeviceTokenHandler(
        IUserDeviceTokenRepository repository,
        ICurrentUserAccessor currentUser,
        ITenantContext tenantContext,
        IUnitOfWork uow)
    {
        _repository = repository;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _uow = uow;
    }

    public async Task<DeviceTokenDto> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ArgumentException("Token is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Platform) || !AllowedPlatforms.Contains(request.Platform))
            throw new ArgumentException("Platform must be ios, android, or web.", nameof(request));

        var tenantId = _tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUser.UserIdOrThrow();
        var utcNow = DateTime.UtcNow;
        var trimmed = request.Token.Trim();

        var existing = await _repository.GetByTokenAsync(tenantId, trimmed, cancellationToken);

        UserDeviceToken entity;
        if (existing is null)
        {
            entity = new UserDeviceToken(
                tenantId,
                userId,
                trimmed,
                request.Platform,
                request.DeviceName,
                request.OsVersion,
                utcNow);
            await _repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            existing.Refresh(request.DeviceName, request.OsVersion, utcNow);
            entity = existing;
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new DeviceTokenDto(
            entity.Id,
            entity.Platform,
            entity.DeviceName,
            entity.OsVersion,
            entity.LastSeenAtUtc,
            entity.CreatedAtUtc);
    }
}

public sealed class DeactivateDeviceTokenHandler : IRequestHandler<DeactivateDeviceTokenCommand, bool>
{
    private readonly IUserDeviceTokenRepository _repository;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _uow;

    public DeactivateDeviceTokenHandler(
        IUserDeviceTokenRepository repository,
        ICurrentUserAccessor currentUser,
        ITenantContext tenantContext,
        IUnitOfWork uow)
    {
        _repository = repository;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _uow = uow;
    }

    public async Task<bool> Handle(DeactivateDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) return false;
        var tenantId = _tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUser.UserIdOrThrow();
        var changed = await _repository.DeactivateAsync(tenantId, userId, request.Token.Trim(), DateTime.UtcNow, cancellationToken);
        if (changed) await _uow.SaveChangesAsync(cancellationToken);
        return changed;
    }
}
