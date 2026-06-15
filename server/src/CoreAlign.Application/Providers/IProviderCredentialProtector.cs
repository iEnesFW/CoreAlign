using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Providers;

public interface IProviderCredentialProtector
{
    string Protect(Guid tenantId, ProviderCategory category, string plaintext);
    TCreds? UnprotectAs<TCreds>(Guid tenantId, ProviderCategory category, string? encryptedJson) where TCreds : class;
    bool TryUnprotect(Guid tenantId, ProviderCategory category, string encryptedJson, out string? plaintext);
}
