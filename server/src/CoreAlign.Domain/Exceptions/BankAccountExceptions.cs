namespace CoreAlign.Domain.Exceptions;

public class BankAccountNotFoundException : NotFoundException
{
    public BankAccountNotFoundException() : base("Bank account not found.") { }
}
