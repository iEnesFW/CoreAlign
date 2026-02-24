namespace CoreAlign.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public int StatusCode { get; }

    protected DomainException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class UserNotFoundException : DomainException
{
    public UserNotFoundException() : base("User not found.", 404) { }
}

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() : base("Invalid email or password.", 401) { }
}

public class AccountLockedException : DomainException
{
    public AccountLockedException(DateTimeOffset lockoutEnd)
        : base($"Account is locked until {lockoutEnd:u}.", 423) { }
}

public class EmailNotVerifiedException : DomainException
{
    public EmailNotVerifiedException() : base("Email address has not been verified.", 403) { }
}

public class TokenExpiredException : DomainException
{
    public TokenExpiredException() : base("Token has expired or is invalid.", 400) { }
}

public class DuplicateEmailException : DomainException
{
    public DuplicateEmailException() : base("An account with this email already exists.", 409) { }
}

public class DuplicateUsernameException : DomainException
{
    public DuplicateUsernameException() : base("This username is already taken.", 409) { }
}

public class AccountDisabledException : DomainException
{
    public AccountDisabledException() : base("This account has been disabled.", 403) { }
}
