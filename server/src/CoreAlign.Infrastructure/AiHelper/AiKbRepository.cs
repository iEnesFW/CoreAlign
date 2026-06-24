using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.AiHelper;

public sealed class AiKbRepository : IAiKbRepository
{
    private readonly CoreAlignDbContext _db;

    public AiKbRepository(CoreAlignDbContext db)
    {
        _db = db;
    }

    public Task<AiKbDocument?> FindAsync(
        AiKbSourceType sourceType,
        string sourceRef,
        string locale,
        CancellationToken ct) =>
        _db.Set<AiKbDocument>()
            .FirstOrDefaultAsync(
                d => d.SourceType == sourceType && d.SourceRef == sourceRef && d.Locale == locale,
                ct);

    public Task RemoveAsync(AiKbDocument document, CancellationToken ct)
    {
        _db.Set<AiKbDocument>().Remove(document);
        return Task.CompletedTask;
    }

    public async Task AddAsync(AiKbDocument document, CancellationToken ct)
    {
        await _db.Set<AiKbDocument>().AddAsync(document, ct).ConfigureAwait(false);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
