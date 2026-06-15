using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class UserConsentRepository : IUserConsentRepository
{
    private readonly CoreAlignDbContext _context;

    public UserConsentRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserConsent consent, CancellationToken cancellationToken = default)
    {
        await _context.UserConsents.AddAsync(consent, cancellationToken);
    }

    public async Task<IReadOnlyList<UserConsent>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CapturedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserConsent?> GetLatestAsync(Guid userId, string purpose, CancellationToken cancellationToken = default)
    {
        var normalized = purpose.Trim().ToLowerInvariant();
        return await _context.UserConsents
            .Where(c => c.UserId == userId && c.Purpose == normalized)
            .OrderByDescending(c => c.CapturedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<UserConsent?> GetByIdAsync(Guid consentId, CancellationToken cancellationToken = default)
    {
        return _context.UserConsents.FirstOrDefaultAsync(c => c.Id == consentId, cancellationToken);
    }

    public async Task WithdrawAsync(Guid consentId, CancellationToken cancellationToken = default)
    {
        var consent = await _context.UserConsents.FirstOrDefaultAsync(c => c.Id == consentId, cancellationToken);
        if (consent is null) return;
        consent.Withdraw(DateTime.UtcNow);
    }
}
