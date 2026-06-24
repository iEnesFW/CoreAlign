using System;
using System.Collections.Generic;
using System.Threading;

namespace CoreAlign.Application.AiHelper;

public sealed record AiHelperQuery(
    string Question,
    string Locale,
    string? RoutePath,
    Guid? TenantId,
    bool IsPublic,
    IReadOnlyList<string> Roles,
    Guid ConversationId,
    Guid? UserId = null,
    string? PageEntityType = null,
    Guid? PageEntityId = null,
    Guid? CustomerId = null);

public sealed record AiHelperSource(string Title, string SourceRef, string SourceType);

public abstract record AiHelperEvent;

public sealed record AiHelperSourcesEvent(IReadOnlyList<AiHelperSource> Sources) : AiHelperEvent;

public sealed record AiHelperTokenEvent(string Text) : AiHelperEvent;

public sealed record AiHelperDoneEvent(Guid AnswerId) : AiHelperEvent;

public interface IAiHelperService
{
    IAsyncEnumerable<AiHelperEvent> AskAsync(AiHelperQuery query, CancellationToken ct);
}
