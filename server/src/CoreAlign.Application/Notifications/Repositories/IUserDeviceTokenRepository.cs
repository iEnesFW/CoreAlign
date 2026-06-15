using CoreAlign.Domain.Entities.Notifications;

namespace CoreAlign.Application.Notifications.Repositories;

public interface IUserDeviceTokenRepository
{
    Task<UserDeviceToken?> GetByTokenAsync(Guid tenantId, string token, CancellationToken ct);
    Task<IReadOnlyList<UserDeviceToken>> ListActiveByUserAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<UserDeviceToken>> ListActiveByCustomerAsync(Guid tenantId, Guid customerId, CancellationToken ct);
    Task AddAsync(UserDeviceToken entity, CancellationToken ct);
    Task<bool> DeactivateAsync(Guid tenantId, Guid userId, string token, DateTime utcNow, CancellationToken ct);
    Task<bool> MarkLastUsedAsync(Guid tenantId, Guid tokenId, DateTime utcNow, CancellationToken ct);
}
