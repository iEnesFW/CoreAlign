namespace CoreAlign.Domain.Exceptions;

public class OrderRevisionNotFoundException : NotFoundException
{
    public OrderRevisionNotFoundException() : base("Order revision not found.") { }
}

public class RequestRevisionForbiddenException : DomainException
{
    public RequestRevisionForbiddenException(string status)
        : base($"Order revisions cannot be requested while the order is in '{status}'.")
    {
    }
}

public class InvalidRevisionStateException : DomainException
{
    public InvalidRevisionStateException(string message) : base(message) { }
}

public class RevisionPersonaNotAuthorizedException : ForbiddenException
{
    public RevisionPersonaNotAuthorizedException(string persona, string action)
        : base($"Persona '{persona}' is not authorized to {action} this revision.")
    {
    }
}

public class DuplicateProposedRevisionException : ConflictException
{
    public DuplicateProposedRevisionException()
        : base("A proposed revision is already pending for this order.")
    {
    }
}
