using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class TenantProviderConfig : TenantEntity, IHasConcurrencyToken
{
    public ProviderCategory Category { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public string? EncryptedCredentialsJson { get; private set; }
    public int EnabledCapabilities { get; private set; }
    public DateTime? LastHealthCheckUtc { get; private set; }
    public ProviderHealthStatus LastHealthStatus { get; private set; } = ProviderHealthStatus.Unknown;
    public string? LastHealthMessage { get; private set; }
    public long ConcurrencyToken { get; private set; }

    void IHasConcurrencyToken.BumpConcurrencyToken() => ConcurrencyToken++;

    protected TenantProviderConfig() { }

    public TenantProviderConfig(
        ProviderCategory category,
        string providerName,
        string? displayName = null,
        bool isDefault = false,
        bool isEnabled = true,
        string? encryptedCredentialsJson = null,
        int enabledCapabilities = 0)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name is required.", nameof(providerName));
        }

        Category = category;
        ProviderName = providerName.Trim();
        DisplayName = displayName?.Trim();
        IsDefault = isDefault;
        IsEnabled = isEnabled;
        EncryptedCredentialsJson = encryptedCredentialsJson;
        EnabledCapabilities = enabledCapabilities;
    }

    public void UpdateCredentials(string? encryptedCredentialsJson)
    {
        EncryptedCredentialsJson = encryptedCredentialsJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateCapabilities(int capabilities)
    {
        EnabledCapabilities = capabilities;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDisplayName(string? displayName)
    {
        DisplayName = displayName?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordHealthCheck(ProviderHealthStatus status, string? message, DateTime utcNow)
    {
        LastHealthStatus = status;
        LastHealthMessage = message;
        LastHealthCheckUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }
}
