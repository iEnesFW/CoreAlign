using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.AiHelper;

public sealed class AiHelperTraceWriter : IAiHelperTraceWriter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiHelperTraceWriter> _logger;

    public AiHelperTraceWriter(IServiceScopeFactory scopeFactory, ILogger<AiHelperTraceWriter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task WriteAsync(AiHelperTrace trace, CancellationToken cancellationToken = default)
    {
        try
        {
            AiHelperMetrics.RecordRetrieval(trace.Locale, trace.IsAnonymous, trace.ChunkCount, trace.TopScore);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            var ct = timeout.Token;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

            db.Set<AiHelperQueryLog>().Add(new AiHelperQueryLog
            {
                Id = trace.Id,
                ConversationId = trace.ConversationId,
                TenantId = trace.TenantId,
                IsAnonymous = trace.IsAnonymous,
                Question = Truncate(trace.Question, 2000),
                AnswerText = Truncate(trace.AnswerText, 16000),
                Locale = Truncate(trace.Locale, 10),
                RoutePath = TruncateNullable(trace.RoutePath, 512),
                ChatModel = Truncate(trace.ChatModel, 64),
                ChunkCount = trace.ChunkCount,
                TopScore = (decimal)Math.Clamp(trace.TopScore, 0d, 1d),
                RetrievedJson = Truncate(trace.RetrievedJson, 16000),
            });

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist AI helper query trace");
        }
    }

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) ? string.Empty : (value.Length <= max ? value : value[..max]);

    private static string? TruncateNullable(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}
