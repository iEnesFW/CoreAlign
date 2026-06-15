using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Privacy;

public class UserAnonymizer : IUserAnonymizer
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserSessionRepository _sessions;
    private readonly IPrivacyEraseService _eraseService;
    private readonly IDataSubjectRequestLog _audit;
    private readonly IPrivacyHasher _hasher;

    public UserAnonymizer(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUserSessionRepository sessions,
        IPrivacyEraseService eraseService,
        IDataSubjectRequestLog audit,
        IPrivacyHasher hasher)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _sessions = sessions;
        _eraseService = eraseService;
        _audit = audit;
        _hasher = hasher;
    }

    public async Task AnonymizeAsync(User user, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var anonId = user.Id.ToString("N");
        var usernameHash = _hasher.Hash(user.TenantId, user.Username);
        var emailHash = _hasher.Hash(user.TenantId, user.Email);
        var originalEmail = user.Email;

        await _refreshTokens.RevokeAllByUserIdAsync(user.Id, cancellationToken);
        await _sessions.RevokeAllByUserIdAsync(user.Id, cancellationToken);
        await _eraseService.EraseUserCascadeAsync(user.Id, originalEmail, nowUtc, cancellationToken);

        user.Email = $"erased-{anonId}@erased.local";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.Username = $"erased-{anonId}";
        user.FirstName = null;
        user.LastName = null;
        user.PhoneNumber = null;
        user.AvatarUrl = null;
        user.IsActive = false;
        user.IsTwoFactorEnabled = false;
        user.TwoFactorSecretKey = null;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        user.UpdatedAtUtc = nowUtc;
        user.ResetSecurityStamp();
        _users.Update(user);

        await _audit.RecordErasureAsync(
            user.TenantId,
            user.Id,
            usernameHash,
            emailHash,
            nowUtc,
            cancellationToken);
    }
}
