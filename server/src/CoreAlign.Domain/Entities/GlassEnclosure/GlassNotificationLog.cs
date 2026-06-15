using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.GlassEnclosure;

public class GlassNotificationLog : TenantEntity
{
    public Guid ProjectId { get; private set; }
    public GlassNotificationEventCode EventCode { get; private set; }
    public GlassNotificationChannel Channel { get; private set; }
    public Guid? TemplateId { get; private set; }
    public GlassNotificationRecipientKind RecipientKind { get; private set; }
    public string RecipientAddress { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public string? ProviderMessageId { get; private set; }
    public GlassNotificationStatus Status { get; private set; } = GlassNotificationStatus.Pending;
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }

    protected GlassNotificationLog() { }

    public GlassNotificationLog(
        Guid projectId,
        GlassNotificationEventCode eventCode,
        GlassNotificationChannel channel,
        GlassNotificationRecipientKind recipientKind,
        string recipientAddress,
        string payloadJson,
        Guid? templateId = null)
    {
        ProjectId = projectId;
        EventCode = eventCode;
        Channel = channel;
        RecipientKind = recipientKind;
        RecipientAddress = recipientAddress;
        PayloadJson = payloadJson;
        TemplateId = templateId;
    }

    public void MarkSent(string? providerMessageId)
    {
        Status = GlassNotificationStatus.Sent;
        ProviderMessageId = providerMessageId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        Status = GlassNotificationStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkRead()
    {
        Status = GlassNotificationStatus.Read;
        ReadAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = GlassNotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount += 1;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
