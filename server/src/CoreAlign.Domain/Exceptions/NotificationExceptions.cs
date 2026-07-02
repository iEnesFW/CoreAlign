namespace CoreAlign.Domain.Exceptions;

public class NotificationMessageNotFoundException : NotFoundException
{
    public NotificationMessageNotFoundException(Guid id)
        : base($"Notification message {id} was not found.") { }
}

public class NotificationMessageAccessForbiddenException : ForbiddenException
{
    public NotificationMessageAccessForbiddenException(Guid id)
        : base($"Notification message {id} is not accessible by the current user.") { }
}
