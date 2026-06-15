using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CoreAlign.Infrastructure.Repositories;

public class ProcessedWebhookEventRepository : IProcessedWebhookEventRepository
{
    private readonly CoreAlignDbContext _context;

    public ProcessedWebhookEventRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(string gateway, string eventId, string eventType, CancellationToken cancellationToken = default)
        => _context.ProcessedWebhookEvents
            .AsNoTracking()
            .AnyAsync(e => e.Gateway == gateway && e.EventId == eventId && e.EventType == eventType, cancellationToken);

    public async Task AddAsync(ProcessedWebhookEvent evt, CancellationToken cancellationToken = default)
    {
        if (evt is null) throw new ArgumentNullException(nameof(evt));
        await _context.ProcessedWebhookEvents.AddAsync(evt, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _context.Entry(evt).State = EntityState.Detached;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == "23505";
}
