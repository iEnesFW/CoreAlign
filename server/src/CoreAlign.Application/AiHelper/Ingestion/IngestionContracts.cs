using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Domain.Entities.AiHelper;

namespace CoreAlign.Application.AiHelper.Ingestion;

public sealed record KbSourceDocument(
    AiKbSourceType SourceType,
    string SourceRef,
    string Title,
    string Locale,
    AiKbScope Scope,
    Guid? TenantId,
    string Content,
    string? RequiredRole = null);

public sealed record IngestionResult(int DocumentCount, int ChunkCount, int SkippedCount);

public interface IKbSourceProvider
{
    Task<IReadOnlyList<KbSourceDocument>> GetSourcesAsync(CancellationToken ct);
}

public interface IKnowledgeIngestionService
{
    Task<IngestionResult> ReindexAsync(CancellationToken ct);
}
