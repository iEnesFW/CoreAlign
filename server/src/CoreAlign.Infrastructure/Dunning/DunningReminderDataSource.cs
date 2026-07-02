using System.Text.Json;
using CoreAlign.Application.Dunning;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Dunning;

public sealed class DunningReminderDataSource : IDunningReminderDataSource
{
    private static readonly InvoiceStatus[] RemindableInvoiceStatuses =
    {
        InvoiceStatus.Issued,
        InvoiceStatus.Sent,
        InvoiceStatus.PartiallyPaid,
        InvoiceStatus.Overdue
    };

    private readonly CoreAlignDbContext _context;

    public DunningReminderDataSource(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<DunningSettingSnapshot>> GetEnabledSettingsAsync(CancellationToken ct = default)
    {
        var rows = await _context.DunningSettings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.IsEnabled)
            .Select(d => new { d.TenantId, d.Type, d.SendInApp, d.SendEmail, d.RecipientUserIdsJson })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .Select(d => new DunningSettingSnapshot(
                d.TenantId,
                d.Type,
                d.SendInApp,
                d.SendEmail,
                Deserialize(d.RecipientUserIdsJson)))
            .ToList();
    }

    public async Task<IReadOnlyList<DueInvoiceReminder>> GetDueInvoicesAsync(
        Guid tenantId,
        DateTime dueOnOrBeforeUtc,
        int max,
        CancellationToken ct = default)
    {
        return await _context.Invoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId
                && RemindableInvoiceStatuses.Contains(i.Status)
                && i.Total > i.AmountPaid
                && i.DueDate <= dueOnOrBeforeUtc)
            .OrderBy(i => i.DueDate)
            .Take(max)
            .Select(i => new DueInvoiceReminder(i.Id, i.InvoiceNumber, i.DueDate, i.Total - i.AmountPaid, i.Currency))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExpiringQuoteReminder>> GetExpiringQuotesAsync(
        Guid tenantId,
        DateTime nowUtc,
        DateTime expiringOnOrBeforeUtc,
        int max,
        CancellationToken ct = default)
    {
        return await _context.Quotes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(q => q.TenantId == tenantId
                && q.Status == QuoteStatus.Sent
                && q.ValidUntilUtc > nowUtc
                && q.ValidUntilUtc <= expiringOnOrBeforeUtc)
            .OrderBy(q => q.ValidUntilUtc)
            .Take(max)
            .Select(q => new ExpiringQuoteReminder(q.Id, q.QuoteNumber, q.ValidUntilUtc, q.Currency))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<CriticalStockReminder>> GetCriticalStockAsync(
        Guid tenantId,
        int max,
        CancellationToken ct = default)
    {
        return await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId
                && p.IsStockTracked
                && p.ReorderPoint > 0
                && p.StockQuantity <= p.ReorderPoint)
            .OrderBy(p => p.StockQuantity)
            .Take(max)
            .Select(p => new CriticalStockReminder(p.Id, p.Sku, p.Name, p.StockQuantity, p.ReorderPoint))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<Guid> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<Guid>();
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }
}
