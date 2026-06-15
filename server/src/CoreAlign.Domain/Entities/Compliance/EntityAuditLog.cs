using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Compliance;

public enum EntityAuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
}

public class EntityAuditLog : TenantEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public EntityAuditAction Action { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public Guid? UserId { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; set; }
    public string RollingHash { get; set; } = string.Empty;
    public long Sequence { get; set; }

    public static string ComputeRollingHash(string? previousHash, Guid tenantId, string entityType, Guid entityId, EntityAuditAction action, string? beforeJson, string? afterJson, Guid? userId, DateTime changedAtUtc, long sequence)
    {
        var canonical = string.Concat(
            previousHash ?? string.Empty,
            "|", tenantId.ToString("N"),
            "|", entityType,
            "|", entityId.ToString("N"),
            "|", ((int)action).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "|", beforeJson ?? string.Empty,
            "|", afterJson ?? string.Empty,
            "|", userId?.ToString("N") ?? string.Empty,
            "|", changedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            "|", sequence.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var bytes = System.Text.Encoding.UTF8.GetBytes(canonical);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
