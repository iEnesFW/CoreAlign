namespace CoreAlign.Domain.Exceptions;

public class SsoProviderNotFoundException : NotFoundException
{
    public SsoProviderNotFoundException() : base("SSO identity provider not found.") { }
}

public class SsoProviderDuplicateException : ConflictException
{
    public SsoProviderDuplicateException() : base("An SSO identity provider with this name already exists.") { }
}

public class SsoAssertionInvalidException : AuthenticationException
{
    public SsoAssertionInvalidException(string message) : base(message) { }
}

public class SsoProviderInactiveException : ForbiddenException
{
    public SsoProviderInactiveException() : base("This SSO identity provider is not currently active.") { }
}
