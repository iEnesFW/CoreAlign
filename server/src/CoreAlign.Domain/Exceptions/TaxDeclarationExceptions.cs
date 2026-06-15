namespace CoreAlign.Domain.Exceptions;

public class TaxDeclarationNotFoundException : NotFoundException
{
    public TaxDeclarationNotFoundException() : base("Tax declaration not found.") { }
}

public class TaxDeclarationInvalidStateException : DomainException
{
    public TaxDeclarationInvalidStateException(string fromStatus, string action)
        : base($"Cannot {action} tax declaration in status '{fromStatus}'.")
    {
    }
}

public class TaxDeclarationRejectionReasonRequiredException : DomainException
{
    public TaxDeclarationRejectionReasonRequiredException()
        : base("A rejection reason is required when marking a declaration rejected.")
    {
    }
}
