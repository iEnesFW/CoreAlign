using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class DataSubjectRequestRepository : IDataSubjectRequestRepository
{
    private readonly CoreAlignDbContext _context;

    public DataSubjectRequestRepository(CoreAlignDbContext context) => _context = context;

    public Task<DataSubjectRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.DataSubjectRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DataSubjectRequest>> ListAsync(
        DataSubjectRequestStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataSubjectRequests.AsNoTracking();
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return await query
            .OrderByDescending(r => r.SubmittedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(DataSubjectRequestStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.DataSubjectRequests.AsNoTracking();
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(DataSubjectRequest entity, CancellationToken cancellationToken = default) =>
        await _context.DataSubjectRequests.AddAsync(entity, cancellationToken);

    public void Update(DataSubjectRequest entity) => _context.DataSubjectRequests.Update(entity);
}
