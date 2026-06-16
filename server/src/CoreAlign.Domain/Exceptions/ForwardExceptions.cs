namespace CoreAlign.Domain.Exceptions;

public abstract class RateLimitExceededException : DomainException
{
    protected RateLimitExceededException(string message) : base(message) { }
}

public class DocumentForwardRateLimitExceededException : RateLimitExceededException
{
    public DocumentForwardRateLimitExceededException()
        : base("Document forward rate limit reached. Please try again shortly.") { }
}
