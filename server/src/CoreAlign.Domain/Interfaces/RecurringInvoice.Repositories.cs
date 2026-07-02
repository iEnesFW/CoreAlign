using CoreAlign.Domain.Entities.Invoices;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IRecurringInvoiceTemplateRepository
{
    Task<RecurringInvoiceTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RecurringInvoiceTemplate?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RecurringInvoiceTemplate> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        RecurringInvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(RecurringInvoiceTemplate template, CancellationToken cancellationToken = default);

    void Update(RecurringInvoiceTemplate template);
}
