using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly CoreAlignDbContext _context;

    public UserPreferencesRepository(CoreAlignDbContext context) => _context = context;

    public Task<UserPreferences?> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task AddAsync(UserPreferences preferences, CancellationToken cancellationToken = default) =>
        await _context.UserPreferences.AddAsync(preferences, cancellationToken);

    public void Update(UserPreferences preferences) => _context.UserPreferences.Update(preferences);
}
