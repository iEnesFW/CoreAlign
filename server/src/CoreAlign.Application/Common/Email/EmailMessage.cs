namespace CoreAlign.Application.Common.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string BodyHtml,
    string? BodyText,
    string? ReplyTo,
    Guid TenantId);
