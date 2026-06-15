using CoreAlign.Domain.Entities.Customers;

namespace CoreAlign.Application.Customers.Merge;

public interface ICustomerMergeOperationRepository
{
    Task<CustomerMergeLog?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerMergeLog log, CancellationToken cancellationToken = default);
}

public interface ICustomerMergeReassignmentService
{
    Task<CustomerMergeCounts> ReassignAsync(Guid sourceCustomerId, Guid targetCustomerId, CancellationToken cancellationToken = default);
}

public sealed record CustomerMergeCounts(
    int Orders,
    int Invoices,
    int Payments,
    int Addresses,
    int Contacts,
    int Comments,
    int LedgerEntries,
    int Transactions,
    int TagLinks,
    int DealerLinks,
    int CustomerUsers,
    int Other);
