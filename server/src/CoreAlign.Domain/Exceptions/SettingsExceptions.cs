namespace CoreAlign.Domain.Exceptions;

public class TenantNotFoundException : NotFoundException
{
    public TenantNotFoundException() : base("Tenant not found for the current context.") { }
}

public class EmailTemplateNotFoundException : NotFoundException
{
    public EmailTemplateNotFoundException(Guid id) : base($"Email template {id} not found.") { }
}

public class EmailTemplateConflictException : ConflictException
{
    public EmailTemplateConflictException(string code, string locale)
        : base($"An email template with code '{code}' and locale '{locale}' already exists.") { }
}
