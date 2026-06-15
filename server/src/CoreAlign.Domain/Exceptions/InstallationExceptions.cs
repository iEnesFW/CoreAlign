namespace CoreAlign.Domain.Exceptions;

public class FileOwnershipViolationException : ForbiddenException
{
    public FileOwnershipViolationException()
        : base("File does not belong to the current tenant or scope.") { }
}

public class ServiceTicketCustomerOwnershipException : ForbiddenException
{
    public ServiceTicketCustomerOwnershipException()
        : base("Authenticated user is not authorized to open a ticket for the requested customer.") { }
}
