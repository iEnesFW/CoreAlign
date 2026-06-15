namespace CoreAlign.Domain.Exceptions;

public class QuoteNotFoundException : NotFoundException
{
    public QuoteNotFoundException() : base("Quote not found.") { }
}

public class DuplicateQuoteNumberException : ConflictException
{
    public DuplicateQuoteNumberException() : base("A quote with this number already exists.") { }
}

public class InvalidQuoteStatusTransitionException : DomainException
{
    public InvalidQuoteStatusTransitionException(string fromStatus, string toStatus)
        : base($"Cannot transition quote from {fromStatus} to {toStatus}.")
    {
    }
}

public class QuoteImmutableException : DomainException
{
    public QuoteImmutableException(string status)
        : base($"Quote header and lines can only be modified while in Draft status (current: {status}).")
    {
    }
}

public class InvalidQuoteLineException : DomainException
{
    public InvalidQuoteLineException(string message) : base(message) { }
}

public class QuoteAlreadyConvertedException : ConflictException
{
    public QuoteAlreadyConvertedException()
        : base("This quote has already been converted to an order.") { }
}
