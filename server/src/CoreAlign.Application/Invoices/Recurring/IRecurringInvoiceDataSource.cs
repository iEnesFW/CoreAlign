namespace CoreAlign.Application.Invoices.Recurring;

public sealed record DueRecurringTemplateSnapshot(Guid TenantId, Guid TemplateId);

public interface IRecurringInvoiceDataSource
{
    Task<IReadOnlyList<DueRecurringTemplateSnapshot>> GetDueTemplatesAsync(
        DateOnly today,
        int max,
        CancellationToken cancellationToken = default);
}
