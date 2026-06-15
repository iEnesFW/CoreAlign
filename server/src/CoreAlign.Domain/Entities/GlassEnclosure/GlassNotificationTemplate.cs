using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassNotificationTemplate : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public GlassNotificationEventCode EventCode { get; private set; }
    public GlassNotificationChannel Channel { get; private set; } = GlassNotificationChannel.Email;
    public string Locale { get; private set; } = "tr-TR";
    public string? SubjectTemplate { get; private set; }
    public string BodyTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    protected GlassNotificationTemplate() { }

    public GlassNotificationTemplate(
        string code,
        GlassNotificationEventCode eventCode,
        GlassNotificationChannel channel,
        string locale,
        string bodyTemplate,
        string? subjectTemplate = null)
    {
        Code = code;
        EventCode = eventCode;
        Channel = channel;
        Locale = locale;
        BodyTemplate = bodyTemplate;
        SubjectTemplate = subjectTemplate;
    }

    public void Update(
        GlassNotificationEventCode eventCode,
        GlassNotificationChannel channel,
        string locale,
        string? subjectTemplate,
        string bodyTemplate,
        bool isActive)
    {
        EventCode = eventCode;
        Channel = channel;
        Locale = locale;
        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
