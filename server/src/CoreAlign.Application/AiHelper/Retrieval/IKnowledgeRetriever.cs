using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Domain.Entities.AiHelper;

namespace CoreAlign.Application.AiHelper.Retrieval;

public sealed record KnowledgeChunk(
    Guid DocumentId,
    string Title,
    string SourceRef,
    AiKbSourceType SourceType,
    string Content,
    double Score);

public sealed record RetrievalQuery(
    float[] QueryEmbedding,
    string Locale,
    Guid? TenantId,
    IReadOnlyList<string> Roles,
    int MaxChunks,
    double MinScore);

public interface IKnowledgeRetriever
{
    Task<IReadOnlyList<KnowledgeChunk>> RetrieveAsync(RetrievalQuery query, CancellationToken ct);
}
