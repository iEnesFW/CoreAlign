using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class FeedbackTicketComment : TenantEntity
{
    public const int MaxBodyLength = 4000;

    public Guid FeedbackTicketId { get; private set; }
    public Guid? AuthorUserId { get; private set; }
    public string? AuthorName { get; private set; }
    public string Body { get; private set; } = string.Empty;
    // An internal note is visible only to a platform administrator; the reporter is never told it exists.
    public bool IsInternal { get; private set; }

    protected FeedbackTicketComment() { }

    public FeedbackTicketComment(
        Guid feedbackTicketId,
        string body,
        Guid? authorUserId,
        string? authorName,
        bool isInternal)
    {
        var trimmed = (body ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Comment body is required.", nameof(body));
        }
        FeedbackTicketId = feedbackTicketId;
        Body = trimmed.Length > MaxBodyLength ? trimmed[..MaxBodyLength] : trimmed;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        IsInternal = isInternal;
    }
}
