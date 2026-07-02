namespace CoreAlign.Domain.Exceptions;

public class RecurringInvoiceTemplateNotFoundException : NotFoundException
{
    public RecurringInvoiceTemplateNotFoundException() : base("Recurring invoice template not found.") { }
}

public class InvalidRecurringInvoiceTransitionException : ConflictException
{
    public InvalidRecurringInvoiceTransitionException(string message) : base(message) { }
}
