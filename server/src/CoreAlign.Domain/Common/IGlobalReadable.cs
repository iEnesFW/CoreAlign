namespace CoreAlign.Domain.Common;

/// <summary>
/// Marker for tenant-owned entities that are also globally readable (system rows
/// with TenantId = Guid.Empty). It exempts the entity from the strict tenant
/// foreign-key constraint so Guid.Empty rows can be persisted; the DbContext query
/// filter itself stays strict per-tenant, so global rows are reached only by
/// repositories that explicitly IgnoreQueryFilters() and add a
/// "TenantId == Guid.Empty" predicate (see ExchangeRateRepository and
/// PayrollParametersRepository).
/// </summary>
public interface IGlobalReadable
{
}
