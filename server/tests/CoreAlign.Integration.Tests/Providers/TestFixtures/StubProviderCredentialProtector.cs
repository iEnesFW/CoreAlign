using System.Text.Json;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Integration.Tests.Providers.TestFixtures;

/// <summary>
/// Pass-through credential protector — tests want to assert provider behavior,
/// not DataProtection key rotation. Stores credentials as plain JSON; production
/// uses an AES-GCM-backed implementation behind the same interface.
/// </summary>
public sealed class StubProviderCredentialProtector : IProviderCredentialProtector
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Protect(Guid tenantId, ProviderCategory category, string plaintext) => plaintext;

    public TCreds? UnprotectAs<TCreds>(Guid tenantId, ProviderCategory category, string? encryptedJson)
        where TCreds : class
    {
        if (string.IsNullOrWhiteSpace(encryptedJson))
        {
            return null;
        }
        return JsonSerializer.Deserialize<TCreds>(encryptedJson, JsonOptions);
    }

    public bool TryUnprotect(Guid tenantId, ProviderCategory category, string encryptedJson, out string? plaintext)
    {
        plaintext = encryptedJson;
        return true;
    }
}
