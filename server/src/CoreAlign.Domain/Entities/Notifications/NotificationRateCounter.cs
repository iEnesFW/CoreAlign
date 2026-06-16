using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities.Notifications;

public class NotificationRateCounter : TenantEntity
{
    public string ProviderName { get; private set; } = string.Empty;
    public RateScope Scope { get; private set; }
    public string ScopeKey { get; private set; } = string.Empty;
    public DateTime WindowStartUtc { get; private set; }
    public int Count { get; private set; }

    protected NotificationRateCounter() { }

    public NotificationRateCounter(Guid tenantId, string providerName, RateScope scope, string scopeKey, DateTime windowStartUtc)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentException("ProviderName is required.", nameof(providerName));

        TenantId = tenantId;
        ProviderName = providerName.Trim();
        Scope = scope;
        ScopeKey = scopeKey ?? string.Empty;
        WindowStartUtc = windowStartUtc;
        Count = 0;
    }

    public void Increment()
    {
        Count++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
