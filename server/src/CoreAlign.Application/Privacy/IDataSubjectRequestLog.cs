namespace CoreAlign.Application.Privacy;

public interface IDataSubjectRequestLog
{
    Task RecordErasureAsync(
        Guid tenantId,
        Guid userId,
        string usernameHash,
        string emailHash,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken = default);

    Task RecordExportAsync(
        Guid tenantId,
        Guid userId,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken = default);
}
