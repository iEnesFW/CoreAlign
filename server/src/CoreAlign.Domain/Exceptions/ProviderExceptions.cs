using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Exceptions;

public class ProviderNotFoundException : DomainException
{
    public string ProviderType { get; }
    public string ProviderName { get; }
    public ProviderNotFoundException(string providerType, string providerName)
        : base("Provider.NotFound") { ProviderType = providerType; ProviderName = providerName; }
}

public class ProviderNotConfiguredException : DomainException
{
    public ProviderCategory Category { get; }
    public Guid TenantId { get; }
    public ProviderNotConfiguredException(ProviderCategory category, Guid tenantId)
        : base("Provider.NotConfigured") { Category = category; TenantId = tenantId; }
}

public class ProviderCredentialDecryptionException : DomainException
{
    public ProviderCredentialDecryptionException(string message)
        : base("Provider.CredentialDecryptionFailed") { }
}
