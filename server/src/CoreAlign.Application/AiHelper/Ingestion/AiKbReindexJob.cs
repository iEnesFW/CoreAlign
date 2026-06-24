using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.AiHelper.Ingestion;

public sealed class AiKbReindexJob
{
    private readonly IKnowledgeIngestionService _ingestion;
    private readonly ILogger<AiKbReindexJob> _logger;

    public AiKbReindexJob(IKnowledgeIngestionService ingestion, ILogger<AiKbReindexJob> logger)
    {
        _ingestion = ingestion;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var result = await _ingestion.ReindexAsync(ct).ConfigureAwait(false);
        _logger.LogInformation(
            "AI Helper scheduled reindex: {Docs} docs, {Chunks} chunks, {Skipped} skipped",
            result.DocumentCount,
            result.ChunkCount,
            result.SkippedCount);
    }
}
