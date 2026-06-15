namespace CoreAlign.Domain.Exceptions;

public class EmailSendFailedException : DomainException
{
    public string Recipient { get; }
    public string Reason { get; }

    public EmailSendFailedException(string recipient, string reason)
        : base($"Failed to send email to {recipient}: {reason}")
    {
        Recipient = recipient;
        Reason = reason;
    }
}
