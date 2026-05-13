namespace CoreAlign.Domain.Exceptions;

public class CustomerNotFoundException : NotFoundException
{
    public CustomerNotFoundException() : base("Customer not found.") { }
}
