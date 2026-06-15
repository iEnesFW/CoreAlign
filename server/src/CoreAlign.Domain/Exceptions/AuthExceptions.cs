namespace CoreAlign.Domain.Exceptions;

public class PasswordReuseException : ConflictException
{
    public PasswordReuseException()
        : base("This password has been used recently. Choose a password you have not used in your last 5 changes.") { }
}

public class CompromisedPasswordException : ConflictException
{
    public CompromisedPasswordException()
        : base("This password has appeared in a known data breach. Choose a different password.") { }
}

public class WeakPasswordException : ConflictException
{
    public WeakPasswordException(string reason)
        : base(reason) { }
}

public class TwoFactorAlreadyEnabledException : ConflictException
{
    public TwoFactorAlreadyEnabledException()
        : base("Two-factor authentication is already enabled for this user.") { }
}

public class TwoFactorNotEnabledException : ConflictException
{
    public TwoFactorNotEnabledException()
        : base("Two-factor authentication is not enabled for this user.") { }
}

public class TwoFactorRequiredException : ConflictException
{
    public TwoFactorRequiredException()
        : base("Two-factor authentication is required by tenant policy for this user.") { }
}

public class InvalidTwoFactorCodeException : AuthenticationException
{
    public InvalidTwoFactorCodeException()
        : base("Invalid two-factor authentication code.") { }
}

public class InvalidTwoFactorChallengeException : AuthenticationException
{
    public InvalidTwoFactorChallengeException()
        : base("Two-factor challenge token is invalid or has expired.") { }
}
