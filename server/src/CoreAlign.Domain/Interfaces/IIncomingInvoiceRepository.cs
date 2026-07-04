using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IIncomingInvoiceRepository
{
    Task AddAsync(IncomingInvoice invoice, CancellationToken cancellationToken = default);

    Task<IncomingInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEttnAsync(string ettn, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncomingInvoice>> ExistingEttnsAsync(IEnumerable<string> ettns, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<IncomingInvoice> Items, int Total)> SearchAsync(
        IncomingInvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
