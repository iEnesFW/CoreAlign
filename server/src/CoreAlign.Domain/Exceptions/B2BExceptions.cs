namespace CoreAlign.Domain.Exceptions;

public class DealerAccountNotFoundException : Exception
{
    public DealerAccountNotFoundException() : base("Dealer account not found.") { }
}

public class CustomerUserNotFoundException : Exception
{
    public CustomerUserNotFoundException() : base("Customer user membership not found.") { }
}

public class DealerUserNotFoundException : Exception
{
    public DealerUserNotFoundException() : base("Dealer user membership not found.") { }
}

public class DealerCustomerLinkNotFoundException : Exception
{
    public DealerCustomerLinkNotFoundException() : base("Dealer-customer link not found.") { }
}

public class DuplicateDealerCodeException : Exception
{
    public DuplicateDealerCodeException() : base("A dealer with this code already exists.") { }
}

public class DuplicateCustomerUserException : Exception
{
    public DuplicateCustomerUserException() : base("The user is already a member of this customer.") { }
}

public class DuplicateDealerUserException : Exception
{
    public DuplicateDealerUserException() : base("The user is already a member of this dealer.") { }
}

public class B2BForbiddenException : Exception
{
    public B2BForbiddenException(string message) : base(message) { }
}

public class PortalScopeNotResolvedException : ForbiddenException
{
    public PortalScopeNotResolvedException(string message) : base(message) { }
}
