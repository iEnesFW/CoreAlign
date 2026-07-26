namespace CoreAlign.Domain.Exceptions;

public sealed class InvalidFeedbackStatusTransitionException : ConflictException
{
    public InvalidFeedbackStatusTransitionException(string from, string to)
        : base($"Feedback ticket cannot move from '{from}' to '{to}'.") { }
}

public sealed class FeedbackAttachmentNotFoundException : NotFoundException
{
    public FeedbackAttachmentNotFoundException() : base("Feedback attachment not found.") { }
}

public sealed class FeedbackAttachmentLimitExceededException : DomainException
{
    public FeedbackAttachmentLimitExceededException(int max)
        : base($"A feedback ticket can carry at most {max} attachments.") { }
}

public sealed class FeedbackCommentForbiddenException : ForbiddenException
{
    public FeedbackCommentForbiddenException()
        : base("Only a platform administrator can write an internal note.") { }
}
