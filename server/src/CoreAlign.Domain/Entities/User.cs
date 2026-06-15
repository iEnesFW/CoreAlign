namespace CoreAlign.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; }
    public bool IsTwoFactorEnabled { get; set; }
    public string? TwoFactorSecretKey { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
    public string? PreferredLocale { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    protected User() { }

    public User(Guid tenantId, string username, string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Username = username;
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        PasswordHash = passwordHash;
    }

    public void ResetSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid().ToString();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordSuccessfulLogin()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        LastLoginAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordFailedLogin(int maxFailedAttempts = 5, int lockoutMinutes = 15)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= maxFailedAttempts)
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.UtcNow;
}
