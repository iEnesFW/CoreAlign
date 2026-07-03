using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.AiHelper.Ingestion;

public sealed class AiKbReindexJob
{
    private readonly IKnowledgeIngestionService _ingestion;
    private readonly AiHelperOptions _options;
    private readonly ILogger<AiKbReindexJob> _logger;

    public AiKbReindexJob(
        IKnowledgeIngestionService ingestion,
        IOptions<AiHelperOptions> options,
        ILogger<AiKbReindexJob> logger)
    {
        _ingestion = ingestion;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("AI Helper disabled; skipping scheduled reindex.");
            return;
        }

        var result = await _ingestion.ReindexAsync(ct).ConfigureAwait(false);
        _logger.LogInformation(
            "AI Helper scheduled reindex: {Docs} docs, {Chunks} chunks, {Skipped} skipped",
            result.DocumentCount,
            result.ChunkCount,
            result.SkippedCount);
    }
}
