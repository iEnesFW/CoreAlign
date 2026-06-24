using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAlign.Application.AiHelper;

public sealed record AiHelperTrace(
    Guid Id,
    Guid ConversationId,
    string Question,
    string AnswerText,
    string Locale,
    Guid? TenantId,
    bool IsAnonymous,
    string? RoutePath,
    string ChatModel,
    int ChunkCount,
    double TopScore,
    string RetrievedJson);

public interface IAiHelperTraceWriter
{
    Task WriteAsync(AiHelperTrace trace, CancellationToken cancellationToken = default);
}
