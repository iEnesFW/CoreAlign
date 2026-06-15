using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Jobs;

public sealed class LogIpAnonymizationJob
{
    private readonly IMaintenanceDataAccess _maintenance;
    private readonly ILogger<LogIpAnonymizationJob> _logger;

    public LogIpAnonymizationJob(IMaintenanceDataAccess maintenance, ILogger<LogIpAnonymizationJob> logger)
    {
        _maintenance = maintenance;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var loginRows = await _maintenance.AnonymizeLoginAuditLogsOlderThanAsync(cutoff, HashValue, cancellationToken);
        var activityRows = await _maintenance.AnonymizeActivityLogsOlderThanAsync(cutoff, HashValue, HashValue, cancellationToken);

        _logger.LogInformation(
            "Log IP anonymization completed. LoginRowsAnonymized={Login}, ActivityRowsAnonymized={Activity}.",
            loginRows, activityRows);
    }

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
