using CoreAlign.Application.Invoices.Recurring;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Invoices;

public sealed class RecurringInvoiceDataSource : IRecurringInvoiceDataSource
{
    private readonly CoreAlignDbContext _context;

    public RecurringInvoiceDataSource(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<DueRecurringTemplateSnapshot>> GetDueTemplatesAsync(
        DateOnly today,
        int max,
        CancellationToken cancellationToken = default)
    {
        return await _context.RecurringInvoiceTemplates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.Status == RecurringInvoiceStatus.Active && t.NextRunDate <= today)
            .OrderBy(t => t.NextRunDate)
            .ThenBy(t => t.Id)
            .Take(max)
            .Select(t => new DueRecurringTemplateSnapshot(t.TenantId, t.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
