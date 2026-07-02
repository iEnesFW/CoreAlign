using System.Globalization;
using CoreAlign.Application.Notifications;
using CoreAlign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Dunning;

public sealed class DunningRemindersJob
{
    private const int InvoiceDueWithinDays = 7;
    private const int QuoteExpiringWithinDays = 7;
    private const int MaxRecordsPerType = 500;
    private const string Locale = "tr";
    private const string CategoryKey = "Dunning";

    private readonly IDunningReminderDataSource _data;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<DunningRemindersJob> _logger;

    public DunningRemindersJob(
        IDunningReminderDataSource data,
        INotificationDispatcher dispatcher,
        ILogger<DunningRemindersJob> logger)
    {
        _data = data;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _data.GetEnabledSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (settings.Count == 0)
        {
            _logger.LogDebug("DunningRemindersJob found no enabled dunning settings.");
            return;
        }

        var now = DateTime.UtcNow;
        var dispatched = 0;

        foreach (var setting in settings)
        {
            var channels = BuildChannels(setting);
            if (channels.Count == 0 || setting.RecipientUserIds.Count == 0)
            {
                continue;
            }

            dispatched += setting.Type switch
            {
                DunningType.InvoiceDueReminder => await RemindInvoicesAsync(setting, channels, now, cancellationToken).ConfigureAwait(false),
                DunningType.QuoteExpiringReminder => await RemindQuotesAsync(setting, channels, now, cancellationToken).ConfigureAwait(false),
                DunningType.StockCriticalReminder => await RemindStockAsync(setting, channels, cancellationToken).ConfigureAwait(false),
                _ => 0
            };
        }

        _logger.LogInformation("DunningRemindersJob dispatched {Dispatched} reminder request(s) across {SettingCount} enabled setting(s).", dispatched, settings.Count);
    }

    private async Task<int> RemindInvoicesAsync(
        DunningSettingSnapshot setting,
        IReadOnlyList<NotificationChannel> channels,
        DateTime now,
        CancellationToken ct)
    {
        var invoices = await _data.GetDueInvoicesAsync(setting.TenantId, now.AddDays(InvoiceDueWithinDays), MaxRecordsPerType, ct).ConfigureAwait(false);
        WarnIfTruncated(invoices.Count, setting.Type, setting.TenantId);

        var dispatched = 0;
        foreach (var invoice in invoices)
        {
            var payload = new Dictionary<string, object?>
            {
                ["invoiceNumber"] = invoice.InvoiceNumber,
                ["dueDate"] = invoice.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["amount"] = invoice.AmountDue.ToString("0.##", CultureInfo.InvariantCulture),
                ["currency"] = invoice.Currency
            };
            dispatched += await DispatchToRecipientsAsync(setting, channels, "Dunning.InvoiceDueReminder", payload, ct).ConfigureAwait(false);
        }

        return dispatched;
    }

    private async Task<int> RemindQuotesAsync(
        DunningSettingSnapshot setting,
        IReadOnlyList<NotificationChannel> channels,
        DateTime now,
        CancellationToken ct)
    {
        var quotes = await _data.GetExpiringQuotesAsync(setting.TenantId, now, now.AddDays(QuoteExpiringWithinDays), MaxRecordsPerType, ct).ConfigureAwait(false);
        WarnIfTruncated(quotes.Count, setting.Type, setting.TenantId);

        var dispatched = 0;
        foreach (var quote in quotes)
        {
            var payload = new Dictionary<string, object?>
            {
                ["quoteNumber"] = quote.QuoteNumber,
                ["validUntil"] = quote.ValidUntilUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["currency"] = quote.Currency
            };
            dispatched += await DispatchToRecipientsAsync(setting, channels, "Dunning.QuoteExpiringReminder", payload, ct).ConfigureAwait(false);
        }

        return dispatched;
    }

    private async Task<int> RemindStockAsync(
        DunningSettingSnapshot setting,
        IReadOnlyList<NotificationChannel> channels,
        CancellationToken ct)
    {
        var items = await _data.GetCriticalStockAsync(setting.TenantId, MaxRecordsPerType, ct).ConfigureAwait(false);
        WarnIfTruncated(items.Count, setting.Type, setting.TenantId);

        var dispatched = 0;
        foreach (var item in items)
        {
            var payload = new Dictionary<string, object?>
            {
                ["sku"] = item.Sku,
                ["productName"] = item.Name,
                ["onHand"] = item.StockQuantity.ToString("0.##", CultureInfo.InvariantCulture),
                ["reorderPoint"] = item.ReorderPoint.ToString("0.##", CultureInfo.InvariantCulture)
            };
            dispatched += await DispatchToRecipientsAsync(setting, channels, "Dunning.StockCriticalReminder", payload, ct).ConfigureAwait(false);
        }

        return dispatched;
    }

    private async Task<int> DispatchToRecipientsAsync(
        DunningSettingSnapshot setting,
        IReadOnlyList<NotificationChannel> channels,
        string templateKey,
        IReadOnlyDictionary<string, object?> payload,
        CancellationToken ct)
    {
        var dispatched = 0;
        foreach (var userId in setting.RecipientUserIds)
        {
            var request = new NotificationRequest(
                setting.TenantId,
                userId,
                null,
                CategoryKey,
                templateKey,
                Locale,
                payload,
                channels);

            try
            {
                await _dispatcher.DispatchAsync(request, ct).ConfigureAwait(false);
                dispatched++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DunningRemindersJob failed to dispatch {TemplateKey} to user {UserId} in tenant {TenantId}", templateKey, userId, setting.TenantId);
            }
        }

        return dispatched;
    }

    private void WarnIfTruncated(int count, DunningType type, Guid tenantId)
    {
        if (count >= MaxRecordsPerType)
        {
            _logger.LogWarning("DunningRemindersJob capped {Type} reminders at {Max} for tenant {TenantId}; remaining records will be reminded on a subsequent run.", type, MaxRecordsPerType, tenantId);
        }
    }

    private static List<NotificationChannel> BuildChannels(DunningSettingSnapshot setting)
    {
        var channels = new List<NotificationChannel>(2);
        if (setting.SendInApp) channels.Add(NotificationChannel.InApp);
        if (setting.SendEmail) channels.Add(NotificationChannel.Email);
        return channels;
    }
}
