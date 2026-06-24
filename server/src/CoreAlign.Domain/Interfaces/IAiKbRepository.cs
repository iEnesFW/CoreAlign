using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Domain.Entities.AiHelper;

namespace CoreAlign.Domain.Interfaces;

public interface IAiKbRepository
{
    Task<AiKbDocument?> FindAsync(AiKbSourceType sourceType, string sourceRef, string locale, CancellationToken ct);

    Task RemoveAsync(AiKbDocument document, CancellationToken ct);

    Task AddAsync(AiKbDocument document, CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
}
