namespace CoreAlign.Domain.Exceptions;

public class InvoiceNotFoundException : NotFoundException
{
    public InvoiceNotFoundException() : base("Invoice not found.") { }
}

public class InvoiceAlreadyExistsForOrderException : ConflictException
{
    public InvoiceAlreadyExistsForOrderException() : base("An invoice already exists for this order.") { }
}

public class OrderNotEligibleForInvoicingException : DomainException
{
    public OrderNotEligibleForInvoicingException(string status)
        : base($"Order in status '{status}' cannot be invoiced. Confirmed, Shipped or Closed required.")
    {
    }
}

public class InvoiceStatusTransitionException : DomainException
{
    public InvoiceStatusTransitionException(string fromStatus, string action)
        : base($"Cannot {action} invoice in status '{fromStatus}'.")
    {
    }
}

public class DuplicateInvoiceNumberException : ConflictException
{
    public DuplicateInvoiceNumberException() : base("An invoice with this number already exists.") { }
}

public class InvalidInvoiceStateException : DomainException
{
    public InvalidInvoiceStateException(string message) : base(message) { }
}

public class InvoiceImmutableException : DomainException
{
    public InvoiceImmutableException(string status)
        : base($"Issued invoices are immutable (current status: {status}). Use a credit note for corrections.")
    {
    }
}

public class CannotIssueEmptyInvoiceException : DomainException
{
    public CannotIssueEmptyInvoiceException() : base("Invoice must have at least one line before issuing.") { }
}

public class PaymentNotFoundException : NotFoundException
{
    public PaymentNotFoundException() : base("Payment not found.") { }
}

public class PaymentApplicationException : DomainException
{
    public PaymentApplicationException(string message) : base(message) { }
}

public class CannotOverApplyPaymentException : DomainException
{
    public CannotOverApplyPaymentException(decimal unapplied, decimal requested)
        : base($"Cannot apply {requested} when only {unapplied} is unapplied.") { }
}

public class CannotOverPayInvoiceException : DomainException
{
    public CannotOverPayInvoiceException(decimal remaining, decimal requested)
        : base($"Cannot apply {requested} to invoice; only {remaining} remains due.") { }
}

public class InvalidInvoiceLineException : DomainException
{
    public InvalidInvoiceLineException(string message) : base(message) { }
}

public class PeriodClosedException : DomainException
{
    public PeriodClosedException(DateTime postingDate)
        : base($"Accounting period for {postingDate:yyyy-MM} is closed.") { }
}
