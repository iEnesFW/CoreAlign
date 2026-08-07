using System.Globalization;

namespace CoreAlign.Application.Billing.Expiry;

public static class ModuleExpiryTemplateKeys
{
    public const string CategoryKey = "Billing";
    public const string Expiring = "Billing.ModuleExpiring";

    public static readonly IReadOnlyList<string> All = [Expiring];
}

/// <summary>A tenant's module grant that is inside the reminder window.</summary>
public sealed record ExpiringModuleSnapshot(
    Guid TenantId,
    Guid TenantModuleId,
    Guid ModuleId,
    string ModuleCode,
    string ModuleName,
    DateTime EndUtc);

public interface IModuleExpiryDataSource
{
    /// <summary>
    /// Cross-tenant by design: the job has no HTTP context, so the global tenant filter would
    /// resolve to Guid.Empty and return nothing. The query therefore ignores the filter and the
    /// caller re-scopes per tenant before dispatching.
    /// </summary>
    Task<IReadOnlyList<ExpiringModuleSnapshot>> GetExpiringAsync(
        DateTime nowUtc,
        int withinDays,
        int max,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetTenantAdminUserIdsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public static class ModuleExpiryThresholds
{
    /// <summary>
    /// Descending so the first threshold a grant qualifies for is the widest one it has not passed
    /// yet; each fires exactly once because the dedup payload carries the threshold.
    /// </summary>
    public static readonly int[] Days = [15, 7, 3, 1];

    public const int WindowDays = 15;

    /// <summary>
    /// The TIGHTEST threshold the grant already qualifies for: 12 days left is a 15-day reminder,
    /// 5 days is a 7-day one, 2 days is a 3-day one. An expired or far-off grant gets nothing.
    /// </summary>
    public static int? ResolveThreshold(DateTime nowUtc, DateTime endUtc)
    {
        if (endUtc <= nowUtc) return null;
        var remaining = (int)Math.Ceiling((endUtc - nowUtc).TotalDays);
        var qualifying = Days.Where(d => remaining <= d).ToArray();
        return qualifying.Length == 0 ? null : qualifying.Min();
    }

    /// <summary>
    /// WHY the payload carries thresholdDays and a date but never "now": the dispatcher dedups on a
    /// hash of the payload. Put a now-derived value in it and the hash changes daily, so the tenant
    /// is reminded every single day; leave the threshold out and the 3-day reminder hashes
    /// identically to the 15-day one and is silently swallowed — the urgent one never arrives.
    /// Key order participates in the hash, so it is fixed here rather than built ad hoc.
    /// </summary>
    public static Dictionary<string, object?> BuildPayload(
        string moduleCode,
        string moduleName,
        DateTime endUtc,
        int thresholdDays) =>
        new()
        {
            ["moduleCode"] = moduleCode,
            ["moduleName"] = moduleName,
            ["expiresOn"] = endUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["thresholdDays"] = thresholdDays.ToString(CultureInfo.InvariantCulture),
        };
}
