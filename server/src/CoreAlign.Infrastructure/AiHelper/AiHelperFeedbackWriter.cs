using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.AiHelper;

public sealed class AiHelperFeedbackWriter : IAiHelperFeedbackWriter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiHelperFeedbackWriter> _logger;

    public AiHelperFeedbackWriter(IServiceScopeFactory scopeFactory, ILogger<AiHelperFeedbackWriter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SubmitAsync(
        Guid answerId,
        bool isHelpful,
        string? reason,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AiHelperMetrics.RecordFeedback(isHelpful);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

            db.Set<AiHelperFeedback>().Add(new AiHelperFeedback
            {
                AnswerId = answerId,
                IsHelpful = isHelpful,
                Reason = Normalize(reason),
                TenantId = tenantId,
            });

            await db.SaveChangesAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist AI helper feedback for answer {AnswerId}", answerId);
        }
    }

    private static string? Normalize(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length <= 1000 ? trimmed : trimmed[..1000];
    }
}
