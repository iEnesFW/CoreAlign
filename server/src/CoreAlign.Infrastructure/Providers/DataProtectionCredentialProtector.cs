using System.Text.Json;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers;

public sealed class DataProtectionCredentialProtector : IProviderCredentialProtector
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<DataProtectionCredentialProtector> _logger;

    public DataProtectionCredentialProtector(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DataProtectionCredentialProtector> logger)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
    }

    public string Protect(Guid tenantId, ProviderCategory category, string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        var protector = GetProtector(tenantId, category);
        return protector.Protect(plaintext);
    }

    public TCreds? UnprotectAs<TCreds>(Guid tenantId, ProviderCategory category, string? encryptedJson) where TCreds : class
    {
        if (string.IsNullOrWhiteSpace(encryptedJson))
        {
            return null;
        }

        if (!TryUnprotect(tenantId, category, encryptedJson, out var plaintext) || plaintext is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TCreds>(plaintext);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Provider credential JSON deserialization failed for tenant {TenantId} category {Category}",
                tenantId,
                category);
            throw new ProviderCredentialDecryptionException("Credential JSON malformed");
        }
    }

    public bool TryUnprotect(Guid tenantId, ProviderCategory category, string encryptedJson, out string? plaintext)
    {
        plaintext = null;
        if (string.IsNullOrWhiteSpace(encryptedJson))
        {
            return false;
        }

        try
        {
            var protector = GetProtector(tenantId, category);
            plaintext = protector.Unprotect(encryptedJson);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Provider credential decryption failed for tenant {TenantId} category {Category}",
                tenantId,
                category);
            return false;
        }
    }

    private IDataProtector GetProtector(Guid tenantId, ProviderCategory category) =>
        _dataProtectionProvider.CreateProtector($"CoreAlign.Provider.{category}.{tenantId:N}");
}
