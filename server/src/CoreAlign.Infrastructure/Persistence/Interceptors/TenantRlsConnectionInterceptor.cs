using System.Data.Common;
using CoreAlign.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Infrastructure.Persistence.Interceptors;

public sealed class TenantRlsConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantRlsConnectionInterceptor(ITenantContext tenantContext) => _tenantContext = tenantContext;

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantGucAsync(connection, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateSetConfigCommand(connection);
        command.ExecuteNonQuery();
    }

    private async Task SetTenantGucAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = CreateSetConfigCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateSetConfigCommand(DbConnection connection)
    {
        var tenantId = _tenantContext.CurrentTenantId ?? Guid.Empty;
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenant, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant";
        parameter.Value = tenantId.ToString();
        command.Parameters.Add(parameter);
        return command;
    }
}
