namespace CoreAlign.Domain.Exceptions;

public class CustomerAddressNotFoundException : NotFoundException
{
    public CustomerAddressNotFoundException() : base("Customer address not found.") { }
}

public class CustomerContactNotFoundException : NotFoundException
{
    public CustomerContactNotFoundException() : base("Customer contact not found.") { }
}
