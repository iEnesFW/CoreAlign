using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class TwoFactorBackupCodeRepository : ITwoFactorBackupCodeRepository
{
    private readonly CoreAlignDbContext _context;

    public TwoFactorBackupCodeRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<TwoFactorBackupCode> codes, CancellationToken cancellationToken = default)
    {
        await _context.TwoFactorBackupCodes.AddRangeAsync(codes, cancellationToken);
    }

    public async Task<IReadOnlyList<TwoFactorBackupCode>> ListActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.TwoFactorBackupCodes
            .Where(c => c.UserId == userId && c.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public Task<TwoFactorBackupCode?> FindActiveByHashAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default)
    {
        return _context.TwoFactorBackupCodes
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CodeHash == codeHash && c.UsedAtUtc == null, cancellationToken);
    }

    public void Update(TwoFactorBackupCode code)
    {
        _context.TwoFactorBackupCodes.Update(code);
    }

    public async Task RemoveAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.TwoFactorBackupCodes
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public class TwoFactorChallengeRepository : ITwoFactorChallengeRepository
{
    private readonly CoreAlignDbContext _context;

    public TwoFactorChallengeRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken = default)
    {
        await _context.TwoFactorChallenges.AddAsync(challenge, cancellationToken);
    }

    public Task<TwoFactorChallenge?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _context.TwoFactorChallenges
            .IgnoreQueryFilters()
            .Include(c => c.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash, cancellationToken);
    }

    public void Update(TwoFactorChallenge challenge)
    {
        _context.TwoFactorChallenges.Update(challenge);
    }
}
