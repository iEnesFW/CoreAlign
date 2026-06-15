using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    void Update(UserPreferences preferences);
}
