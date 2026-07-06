using System.Data.Common;
using CoreAlign.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Infrastructure.Persistence.Interceptors;

// Sets the PostgreSQL GUC that the RLS tenant-isolation policies read (app.tenant_id).
//
// WHY session-scoped (is_local = false) rather than transaction-local: the app issues many reads
// OUTSIDE an explicit transaction, so a transaction-local GUC would be unset for those and RLS
// would deny every row. The GUC is instead set at session scope but RE-SET on EVERY connection
// open. ConnectionOpened fires before any command runs on that (possibly pooled) connection, so a
// pooled connection can never execute a query under a previous checkout's tenant — the leak the
// finding warned about is closed by the re-set, not by is_local. As a second layer, Npgsql's
// default connection reset (DISCARD ALL) clears the GUC when the connection returns to the pool,
// so an idle pooled connection does not sit holding a tenant's context.
//
// The GUC is also explicitly (re)set to the empty tenant when CurrentTenantId is null, so a
// no-tenant context can never inherit a non-empty value.
//
// KNOWN LIMITATION: if a single already-open connection is reused across a mid-flight tenant
// switch (ITenantContext.PushScope without opening a new connection), the GUC keeps the value
// captured at open. Tenant-scanning jobs must therefore use a fresh DI scope (hence a fresh
// connection) per tenant — which they do; otherwise a per-command GUC set would be required.
public sealed class TenantRlsConnectionInterceptor : DbConnectionInterceptor
{
    private static readonly Guid NoTenant = Guid.Empty;

    private readonly ITenantContext _tenantContext;

    public TenantRlsConnectionInterceptor(ITenantContext tenantContext) => _tenantContext = tenantContext;

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateSetConfigCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateSetConfigCommand(connection);
        command.ExecuteNonQuery();
    }

    private DbCommand CreateSetConfigCommand(DbConnection connection)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? NoTenant;
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenant, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant";
        parameter.Value = tenantId.ToString();
        command.Parameters.Add(parameter);
        return command;
    }
}
