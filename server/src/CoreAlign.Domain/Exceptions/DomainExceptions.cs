namespace CoreAlign.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public abstract class NotFoundException : DomainException
{
    protected NotFoundException(string message) : base(message) { }
}

public abstract class ConflictException : DomainException
{
    protected ConflictException(string message) : base(message) { }
}

public abstract class ForbiddenException : DomainException
{
    protected ForbiddenException(string message) : base(message) { }
}

public abstract class AuthenticationException : DomainException
{
    protected AuthenticationException(string message) : base(message) { }
}

public class UserNotFoundException : NotFoundException
{
    public UserNotFoundException() : base("User not found.") { }
}

public class InvalidCredentialsException : AuthenticationException
{
    public InvalidCredentialsException() : base("Invalid email or password.") { }
}

public class AccountLockedException : ForbiddenException
{
    public AccountLockedException(DateTimeOffset lockoutEnd)
        : base($"Account is locked until {lockoutEnd:u}.") { }
}

public class EmailNotVerifiedException : ForbiddenException
{
    public EmailNotVerifiedException() : base("Email address has not been verified.") { }
}

public class TokenExpiredException : AuthenticationException
{
    public TokenExpiredException() : base("Token has expired or is invalid.") { }
}

public class DuplicateEmailException : ConflictException
{
    public DuplicateEmailException() : base("An account with this email already exists.") { }
}

public class DuplicateUsernameException : ConflictException
{
    public DuplicateUsernameException() : base("This username is already taken.") { }
}

public class AccountDisabledException : ForbiddenException
{
    public AccountDisabledException() : base("This account has been disabled.") { }
}

public class TenantInactiveException : ForbiddenException
{
    public TenantInactiveException() : base("This organization's account is inactive.") { }
}

public class CaptchaValidationException : DomainException
{
    public CaptchaValidationException() : base("CAPTCHA verification failed. Please try again.") { }
}

public class MissingTenantContextException : AuthenticationException
{
    public MissingTenantContextException()
        : base("Tenant context is missing or invalid for the current request.") { }
}

public class CrossTenantAccessException : ForbiddenException
{
    public CrossTenantAccessException()
        : base("Resource does not belong to the current tenant.") { }
}

public sealed class FeedbackNotFoundException : NotFoundException
{
    public FeedbackNotFoundException() : base("Feedback ticket not found.") { }
}
