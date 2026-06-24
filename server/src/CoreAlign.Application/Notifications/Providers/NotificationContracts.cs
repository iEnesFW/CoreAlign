namespace CoreAlign.Application.Notifications.Providers;

public sealed record NotificationSendResult(bool Success, string? ProviderMessageId, string? FailureReason)
{
    public static NotificationSendResult Ok(string? providerMessageId = null) => new(true, providerMessageId, null);
    public static NotificationSendResult Fail(string failureReason) => new(false, null, failureReason);
}

public sealed record EmailMessage(
    string From,
    string FromName,
    string To,
    string Subject,
    string BodyHtml,
    string BodyText,
    string? ReplyTo,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null,
    IReadOnlyList<EmailAttachment>? Attachments = null);

public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed record SmsMessage(
    string From,
    string To,
    string Body);

public sealed record PushMessage(
    string DeviceToken,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string>? Data);

public sealed record WhatsAppMessage(
    string From,
    string To,
    string TemplateName,
    string Locale,
    string Body);
