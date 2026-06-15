using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class PasswordHistory : TenantEntity
{
    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;

    protected PasswordHistory() { }

    public PasswordHistory(Guid userId, string passwordHash)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        UserId = userId;
        PasswordHash = passwordHash;
    }
}
