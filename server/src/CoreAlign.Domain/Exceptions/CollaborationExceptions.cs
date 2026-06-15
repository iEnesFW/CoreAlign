namespace CoreAlign.Domain.Exceptions;

public class CommentNotFoundException : NotFoundException
{
    public CommentNotFoundException() : base("Comment not found.") { }
}

public class NotificationNotFoundException : NotFoundException
{
    public NotificationNotFoundException() : base("Notification not found.") { }
}

public class CommentEditForbiddenException : ForbiddenException
{
    public CommentEditForbiddenException() : base("Only the author can edit this comment.") { }
}

public class CommentDeleteForbiddenException : ForbiddenException
{
    public CommentDeleteForbiddenException() : base("Only the author or a tenant administrator can delete this comment.") { }
}

public class NotificationAccessForbiddenException : ForbiddenException
{
    public NotificationAccessForbiddenException() : base("Cannot access another user's notification.") { }
}
