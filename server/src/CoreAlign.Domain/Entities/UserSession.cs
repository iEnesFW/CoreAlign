namespace CoreAlign.Domain.Entities;

public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SessionTokenHash { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }

    public User User { get; set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsActive => !IsExpired && !IsRevoked;

    protected UserSession() { }

    public UserSession(Guid userId, string sessionTokenHash, DateTime expiresAtUtc, string? deviceInfo = null, string? ipAddress = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SessionTokenHash = sessionTokenHash;
        ExpiresAtUtc = expiresAtUtc;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
    }
}
