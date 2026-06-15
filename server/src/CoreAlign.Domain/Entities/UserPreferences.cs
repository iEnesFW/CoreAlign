using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class UserPreferences : ISoftDeletable, IHasConcurrencyToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid TenantId { get; set; }
    public UxComplexityMode? ModeOverride { get; private set; }
    public string? PerScreenOverridesJson { get; private set; }
    public string? LocaleOverride { get; private set; }
    public string? ThemeOverride { get; private set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public string? DeletedReason { get; set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected UserPreferences() { }

    public UserPreferences(Guid userId, Guid tenantId)
    {
        UserId = userId;
        TenantId = tenantId;
    }

    public void SetMode(UxComplexityMode? mode)
    {
        ModeOverride = mode;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPerScreenOverrides(string? json)
    {
        PerScreenOverridesJson = json;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetLocaleOverride(string? locale)
    {
        LocaleOverride = locale;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetThemeOverride(string? theme)
    {
        ThemeOverride = theme;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDeleted(Guid? userId, string? reason, DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedByUserId = userId;
        DeletedReason = reason;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;
        DeletedReason = null;
    }
}
