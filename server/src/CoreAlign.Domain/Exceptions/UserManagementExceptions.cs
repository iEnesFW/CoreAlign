namespace CoreAlign.Domain.Exceptions;

public class CannotDeactivateSelfException : ConflictException
{
    public CannotDeactivateSelfException() : base("You cannot deactivate your own account.") { }
}
