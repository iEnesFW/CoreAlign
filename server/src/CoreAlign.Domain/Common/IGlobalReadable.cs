namespace CoreAlign.Domain.Common;

/// <summary>
/// Marker for tenant-owned entities that are also globally readable (system rows
/// with TenantId = Guid.Empty). The DbContext query filter accepts both the
/// current tenant's rows and global rows, instead of the strict per-tenant default.
/// </summary>
public interface IGlobalReadable
{
}
