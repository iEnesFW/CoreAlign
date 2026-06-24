using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Jobs;

public sealed class ErrorLogRetentionJob
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(90);

    private readonly IErrorLogRepository _repository;
    private readonly ILogger<ErrorLogRetentionJob> _logger;

    public ErrorLogRetentionJob(IErrorLogRepository repository, ILogger<ErrorLogRetentionJob> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var removed = await _repository.DeleteOlderThanAsync(DateTime.UtcNow.Subtract(Retention), cancellationToken);
        if (removed > 0)
        {
            _logger.LogInformation("Error-log retention purged {Count} resolved entries older than {Days} days.", removed, Retention.TotalDays);
        }
    }
}
