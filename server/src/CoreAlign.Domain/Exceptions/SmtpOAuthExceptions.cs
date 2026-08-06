namespace CoreAlign.Domain.Exceptions;

public class SmtpOAuthConfigurationException : DomainException
{
    public SmtpOAuthConfigurationException(string message)
        : base($"SMTP OAuth configuration is invalid: {message}") { }
}

public class SmtpOAuthTokenException : DomainException
{
    public SmtpOAuthTokenException(string message)
        : base($"SMTP OAuth token request failed: {message}") { }
}
