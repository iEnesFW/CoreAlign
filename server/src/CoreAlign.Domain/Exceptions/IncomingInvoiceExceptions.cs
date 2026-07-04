namespace CoreAlign.Domain.Exceptions;

public class IncomingInvoiceNotFoundException : NotFoundException
{
    public IncomingInvoiceNotFoundException() : base("Incoming invoice not found.") { }
}

public class IncomingInvoiceAlreadyProcessedException : ConflictException
{
    public IncomingInvoiceAlreadyProcessedException(string ettn)
        : base($"Incoming invoice {ettn} has already been processed.") { }
}

public class IncomingInvoiceIgnoredException : ConflictException
{
    public IncomingInvoiceIgnoredException(string ettn)
        : base($"Incoming invoice {ettn} was ignored and cannot be processed.") { }
}
