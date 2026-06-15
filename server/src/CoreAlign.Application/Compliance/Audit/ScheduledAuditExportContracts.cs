namespace CoreAlign.Application.Compliance.Audit;

public sealed record ScheduledAuditExportConfig(
    bool Enabled,
    AuditExportFrequency Frequency,
    AuditLogExportFormat Format,
    IReadOnlyList<string> Recipients,
    int LookbackDays,
    IReadOnlyList<string>? EntityTypes,
    DateTime? LastRunAtUtc,
    string? LastRunStatus,
    string? LastRunError);

public enum AuditExportFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
}

public interface IScheduledAuditExportConfigRepository
{
    Task<ScheduledAuditExportConfig?> GetForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task UpsertForTenantAsync(Guid tenantId, ScheduledAuditExportConfig config, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(Guid TenantId, ScheduledAuditExportConfig Config)>> ListEnabledAcrossTenantsAsync(CancellationToken cancellationToken = default);
}
