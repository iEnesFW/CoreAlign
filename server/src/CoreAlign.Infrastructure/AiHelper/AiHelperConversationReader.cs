using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.AiHelper;

public sealed class AiHelperConversationReader : IAiHelperConversationReader
{
    private readonly CoreAlignDbContext _db;

    public AiHelperConversationReader(CoreAlignDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AiHelperConversationTurn>> GetRecentTurnsAsync(
        Guid conversationId,
        Guid? tenantId,
        int maxTurns,
        CancellationToken ct)
    {
        if (conversationId == Guid.Empty || maxTurns <= 0)
        {
            return Array.Empty<AiHelperConversationTurn>();
        }

        var rows = await _db.Set<AiHelperQueryLog>()
            .AsNoTracking()
            .Where(q => q.ConversationId == conversationId && q.TenantId == tenantId)
            .OrderByDescending(q => q.CreatedAtUtc)
            .Take(maxTurns)
            .Select(q => new { q.Question, q.AnswerText, q.CreatedAtUtc })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => new AiHelperConversationTurn(r.Question, r.AnswerText))
            .ToList();
    }
}
