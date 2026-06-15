using CoreAlign.Application.Common.Email;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Compliance.Audit;

public sealed class ScheduledAuditExportJob
{
    public const string EmailTemplateCode = "audit-export-delivery";
    public const string DefaultLocale = "tr-TR";

    private readonly IScheduledAuditExportConfigRepository _configRepository;
    private readonly IAuditLogExportService _exportService;
    private readonly IEmailQueuedOutbox _emailOutbox;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ScheduledAuditExportJob> _logger;

    public ScheduledAuditExportJob(
        IScheduledAuditExportConfigRepository configRepository,
        IAuditLogExportService exportService,
        IEmailQueuedOutbox emailOutbox,
        ITenantContext tenant,
        IUnitOfWork uow,
        ILogger<ScheduledAuditExportJob> logger)
    {
        _configRepository = configRepository;
        _exportService = exportService;
        _emailOutbox = emailOutbox;
        _tenant = tenant;
        _uow = uow;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var schedules = await _configRepository.ListEnabledAcrossTenantsAsync(cancellationToken);
        if (schedules.Count == 0)
        {
            _logger.LogDebug("ScheduledAuditExportJob found no enabled schedules at {NowUtc:o}.", nowUtc);
            return;
        }

        var processed = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var (tenantId, config) in schedules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsDue(config, nowUtc))
            {
                skipped++;
                continue;
            }

            using var scope = _tenant.PushScope(tenantId);
            try
            {
                await ProcessTenantAsync(tenantId, config, nowUtc, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex,
                    "ScheduledAuditExportJob failed for tenant {TenantId}.", tenantId);
                await RecordOutcomeAsync(tenantId, config, nowUtc, "Failed", Truncate(ex.Message), cancellationToken);
            }
        }

        _logger.LogInformation(
            "ScheduledAuditExportJob processed {Processed} of {Total} schedules ({Failed} failed, {Skipped} skipped) at {NowUtc:o}.",
            processed, schedules.Count, failed, skipped, nowUtc);
    }

    internal static bool IsDue(ScheduledAuditExportConfig config, DateTime nowUtc)
    {
        if (!config.Enabled) return false;
        if (!config.LastRunAtUtc.HasValue) return true;
        var interval = ComputeInterval(config.Frequency);
        return (nowUtc - config.LastRunAtUtc.Value) >= interval;
    }

    private static TimeSpan ComputeInterval(AuditExportFrequency frequency) => frequency switch
    {
        AuditExportFrequency.Daily => TimeSpan.FromDays(1),
        AuditExportFrequency.Weekly => TimeSpan.FromDays(7),
        AuditExportFrequency.Monthly => TimeSpan.FromDays(30),
        _ => TimeSpan.FromDays(7),
    };

    private async Task ProcessTenantAsync(
        Guid tenantId,
        ScheduledAuditExportConfig config,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (config.Recipients.Count == 0)
        {
            _logger.LogWarning(
                "ScheduledAuditExportJob: tenant {TenantId} schedule has no recipients; skipping.", tenantId);
            await RecordOutcomeAsync(tenantId, config, nowUtc, "Skipped", "No recipients", cancellationToken);
            return;
        }

        var lookbackDays = Math.Max(1, config.LookbackDays);
        var filter = new AuditLogExportFilter(
            FromUtc: nowUtc.AddDays(-lookbackDays),
            ToUtc: nowUtc,
            EntityTypes: config.EntityTypes,
            Actions: null,
            UserId: null,
            EntityId: null);

        var exportResult = await _exportService.ExportAsync(filter, config.Format, cancellationToken);

        foreach (var recipient in config.Recipients)
        {
            if (string.IsNullOrWhiteSpace(recipient)) continue;
            var context = new Dictionary<string, object?>
            {
                ["tenantId"] = tenantId,
                ["format"] = config.Format.ToString(),
                ["fileName"] = exportResult.FileName,
                ["rowCount"] = exportResult.RowCount,
                ["lookbackDays"] = lookbackDays,
                ["periodFromUtc"] = filter.FromUtc,
                ["periodToUtc"] = filter.ToUtc,
                ["generatedAtUtc"] = nowUtc,
            };
            await _emailOutbox.EnqueueAsync(new EmailQueuedPayload(
                To: recipient,
                TemplateCode: EmailTemplateCode,
                Locale: DefaultLocale,
                TenantId: tenantId,
                ReplyTo: null,
                Context: context), cancellationToken);
        }

        await RecordOutcomeAsync(tenantId, config, nowUtc, "Ok", null, cancellationToken);
    }

    private async Task RecordOutcomeAsync(
        Guid tenantId,
        ScheduledAuditExportConfig config,
        DateTime nowUtc,
        string status,
        string? error,
        CancellationToken cancellationToken)
    {
        var updated = new ScheduledAuditExportConfig(
            config.Enabled,
            config.Frequency,
            config.Format,
            config.Recipients,
            config.LookbackDays,
            config.EntityTypes,
            nowUtc,
            status,
            error);
        await _configRepository.UpsertForTenantAsync(tenantId, updated, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string text) =>
        text.Length > 1900 ? text[..1900] : text;
}
