using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Notifications;

public sealed class UserDeviceToken : TenantEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? OsVersion { get; private set; }
    public DateTime LastSeenAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;

    private UserDeviceToken() { }

    public UserDeviceToken(
        Guid tenantId,
        Guid userId,
        string token,
        string platform,
        string? deviceName,
        string? osVersion,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required", nameof(token));
        if (string.IsNullOrWhiteSpace(platform)) throw new ArgumentException("Platform is required", nameof(platform));

        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        UserId = userId;
        Token = token.Trim();
        Platform = platform.Trim().ToLowerInvariant();
        DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim();
        OsVersion = string.IsNullOrWhiteSpace(osVersion) ? null : osVersion.Trim();
        LastSeenAtUtc = utcNow;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        IsActive = true;
    }

    public void Refresh(
        string? deviceName,
        string? osVersion,
        DateTime utcNow)
    {
        if (!string.IsNullOrWhiteSpace(deviceName)) DeviceName = deviceName.Trim();
        if (!string.IsNullOrWhiteSpace(osVersion)) OsVersion = osVersion.Trim();
        LastSeenAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        IsActive = true;
    }

    public void Deactivate(DateTime utcNow)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void MarkLastUsed(DateTime utcNow)
    {
        LastSeenAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
