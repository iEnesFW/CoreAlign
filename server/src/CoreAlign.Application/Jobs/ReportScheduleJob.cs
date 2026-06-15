using System.Text.Json;
using CoreAlign.Application.Common.Email;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Jobs;

public sealed class ReportScheduleJob
{
    public const string EmailTemplateCode = "report-delivery";
    public const string DefaultLocale = "tr-TR";
    internal static readonly TimeSpan RecentRunDedupeWindow = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IReportScheduleRepository _repository;
    private readonly IEmailQueuedOutbox _emailOutbox;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReportScheduleJob> _logger;

    public ReportScheduleJob(
        IReportScheduleRepository repository,
        IEmailQueuedOutbox emailOutbox,
        ITenantContext tenant,
        IUnitOfWork uow,
        ILogger<ReportScheduleJob> logger)
    {
        _repository = repository;
        _emailOutbox = emailOutbox;
        _tenant = tenant;
        _uow = uow;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var due = await _repository.GetDueAsync(nowUtc, cancellationToken);
        if (due.Count == 0)
        {
            _logger.LogDebug("ReportScheduleJob found no due schedules at {NowUtc:o}.", nowUtc);
            return;
        }

        var processed = 0;
        var failed = 0;
        var skipped = 0;
        foreach (var group in due.GroupBy(d => d.TenantId))
        {
            using var scope = _tenant.PushScope(group.Key);
            foreach (var schedule in group)
            {
                if (ShouldSkipForRecentRun(schedule, nowUtc))
                {
                    skipped++;
                    _logger.LogInformation(
                        "ReportScheduleJob skipping {ScheduleId} — already ran within dedupe window at {LastRunAtUtc:o}.",
                        schedule.Id, schedule.LastRunAtUtc);
                    continue;
                }
                try
                {
                    await ProcessAsync(schedule, nowUtc, cancellationToken);
                    processed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "ReportScheduleJob failed to process schedule {ScheduleId} for tenant {TenantId}.",
                        schedule.Id, group.Key);
                    await RecordFailureAsync(schedule, nowUtc, ex, cancellationToken);
                }
            }
        }

        _logger.LogInformation(
            "ReportScheduleJob processed {Processed} of {Total} due schedules ({Failed} failed, {Skipped} skipped) at {NowUtc:o}.",
            processed, due.Count, failed, skipped, nowUtc);
    }

    private static bool ShouldSkipForRecentRun(ReportSchedule schedule, DateTime nowUtc)
    {
        if (!schedule.LastRunAtUtc.HasValue) return false;
        return string.Equals(schedule.LastRunStatus, "Ok", StringComparison.OrdinalIgnoreCase)
            && (nowUtc - schedule.LastRunAtUtc.Value) < RecentRunDedupeWindow;
    }

    private async Task RecordFailureAsync(ReportSchedule schedule, DateTime nowUtc, Exception ex, CancellationToken cancellationToken)
    {
        var retryAt = nowUtc.Add(TimeSpan.FromMinutes(15));
        var truncated = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
        _uow.ClearChangeTracker();
        var fresh = await _repository.GetByIdAsync(schedule.Id, cancellationToken);
        if (fresh is null) return;
        fresh.RecordRun(nowUtc, "Failed", truncated, retryAt);
        _repository.Update(fresh);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessAsync(ReportSchedule schedule, DateTime nowUtc, CancellationToken cancellationToken)
    {
        await using var transaction = await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var recipients = JsonSerializer.Deserialize<List<string>>(schedule.RecipientsJson, JsonOptions) ?? new List<string>();
            if (recipients.Count == 0)
            {
                _logger.LogWarning("Skipping schedule {ScheduleId} — no recipients configured.", schedule.Id);
            }
            else
            {
                foreach (var recipient in recipients)
                {
                    if (string.IsNullOrWhiteSpace(recipient)) continue;
                    var context = new Dictionary<string, object?>
                    {
                        ["scheduleName"] = schedule.Name,
                        ["reportKey"] = schedule.ReportKey,
                        ["customReportId"] = schedule.CustomReportDefinitionId,
                        ["format"] = schedule.Format.ToString(),
                        ["filtersJson"] = schedule.FiltersJson,
                        ["runAtUtc"] = nowUtc,
                    };
                    await _emailOutbox.EnqueueAsync(new EmailQueuedPayload(
                        To: recipient,
                        TemplateCode: EmailTemplateCode,
                        Locale: DefaultLocale,
                        TenantId: schedule.TenantId,
                        ReplyTo: null,
                        Context: context), cancellationToken);
                }
            }

            var nextRun = ReportSchedule.ComputeNextRunAtUtc(schedule.Frequency, nowUtc);
            schedule.RecordRun(nowUtc, "Ok", null, nextRun);
            _repository.Update(schedule);
            await _uow.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
