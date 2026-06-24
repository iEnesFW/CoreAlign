using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications.Delivery;

public sealed record NotificationChannelSendPayload(
    Guid TenantId,
    Guid NotificationMessageId,
    NotificationChannel Channel,
    string Address,
    string? Subject,
    string BodyHtml,
    string BodyText,
    string? ReplyTo = null,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null,
    IReadOnlyList<EmailAttachmentPayload>? Attachments = null,
    IReadOnlyDictionary<string, string>? PushData = null,
    Guid? DeviceTokenId = null);

public sealed record EmailAttachmentPayload(
    string FileName,
    string ContentType,
    string ContentBase64);
