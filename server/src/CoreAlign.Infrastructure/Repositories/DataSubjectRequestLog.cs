using CoreAlign.Application.Privacy;
using CoreAlign.Domain.Entities;
using CoreAlign.Infrastructure.Persistence;

namespace CoreAlign.Infrastructure.Repositories;

public class DataSubjectRequestLog : IDataSubjectRequestLog
{
    private readonly CoreAlignDbContext _context;

    public DataSubjectRequestLog(CoreAlignDbContext context) => _context = context;

    public async Task RecordErasureAsync(
        Guid tenantId,
        Guid userId,
        string usernameHash,
        string emailHash,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entry = new DataSubjectRequest(
            tenantId,
            userId,
            DataSubjectRequestType.Erasure,
            requestedAtUtc,
            usernameHash,
            emailHash);
        await _context.DataSubjectRequests.AddAsync(entry, cancellationToken);
    }

    public async Task RecordExportAsync(
        Guid tenantId,
        Guid userId,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entry = new DataSubjectRequest(
            tenantId,
            userId,
            DataSubjectRequestType.Export,
            requestedAtUtc,
            null,
            null);
        await _context.DataSubjectRequests.AddAsync(entry, cancellationToken);
    }
}
