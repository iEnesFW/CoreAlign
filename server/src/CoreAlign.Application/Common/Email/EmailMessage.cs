namespace CoreAlign.Application.Common.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string BodyHtml,
    string? BodyText,
    string? ReplyTo,
    Guid TenantId,
    IReadOnlyList<EmailAttachment>? Attachments = null);

public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);
