using CoreAlign.Application.AiHelper.Ingestion;
using CoreAlign.Application.Compliance.Audit;
using CoreAlign.Application.Customers.Maintenance;
using CoreAlign.Application.Dunning;
using CoreAlign.Application.Invoices.Recurring.Jobs;
using CoreAlign.Application.Jobs;
using CoreAlign.Application.Sales.OrderTemplates.Jobs;
using CoreAlign.Application.Treasury.Fx;
using Hangfire;

namespace CoreAlign.API.Hangfire;

public static class RecurringJobsRegistration
{
    public static void RegisterAll(IServiceProvider services)
    {
        var manager = services.GetRequiredService<IRecurringJobManager>();

        manager.AddOrUpdate<OutboxDrainJob>(
            "outbox-drain",
            job => job.RunAsync(CancellationToken.None),
            "*/30 * * * * *");

        manager.AddOrUpdate<TokenCleanupJob>(
            "token-cleanup",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(3));

        manager.AddOrUpdate<LogIpAnonymizationJob>(
            "log-ip-anonymize",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(4));

        manager.AddOrUpdate<QuoteExpiryJob>(
            "quote-expiry",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(2));

        manager.AddOrUpdate<RecurringOrderJob>(
            "recurring-orders",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly());

        manager.AddOrUpdate<TcmbFxIngestJob>(
            "tcmb-fx-ingest",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(8));

        manager.AddOrUpdate<PostFxRevaluationJob>(
            "fx-revaluation-month-end",
            job => job.RunAsync(DateTime.UtcNow, CancellationToken.None),
            "0 1 L * *");

        manager.AddOrUpdate<ReportScheduleJob>(
            "report-schedules",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly());

        manager.AddOrUpdate<ScheduledAuditExportJob>(
            "scheduled-audit-exports",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(5));

        manager.AddOrUpdate<RateCounterCleanupJob>(
            "notification-rate-counter-cleanup",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly());

        manager.AddOrUpdate<ErrorLogRetentionJob>(
            "error-log-retention",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(4));

        manager.AddOrUpdate<AiKbReindexJob>(
            "ai-kb-reindex",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(6));

        manager.AddOrUpdate<DunningRemindersJob>(
            "dunning-reminders",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(1));

        manager.AddOrUpdate<RecurringInvoiceGenerationJob>(
            "recurring-invoice-generation",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(7));

        manager.AddOrUpdate<CustomerBalanceRecomputeJob>(
            "customer-balance-recompute",
            job => job.RunAsync(CancellationToken.None),
            "30 2 * * *");
    }
}
