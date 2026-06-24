using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAlign.Application.AiHelper;

public sealed record AiHelperConversationTurn(string Question, string Answer);

public interface IAiHelperConversationReader
{
    Task<IReadOnlyList<AiHelperConversationTurn>> GetRecentTurnsAsync(
        Guid conversationId,
        Guid? tenantId,
        int maxTurns,
        CancellationToken ct);
}
