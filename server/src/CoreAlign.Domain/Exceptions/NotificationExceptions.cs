namespace CoreAlign.Domain.Exceptions;

public class NotificationMessageNotFoundException : NotFoundException
{
    public NotificationMessageNotFoundException(Guid id)
        : base($"Notification message {id} was not found.") { }
}
