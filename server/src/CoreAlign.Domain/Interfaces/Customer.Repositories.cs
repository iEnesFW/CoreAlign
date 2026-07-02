using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, Customer>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Customer> Items, int Total)> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
    void Remove(Customer customer);
    Task<(int OrderCount, decimal OrderTotal)> GetOrderTotalsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<(int InvoiceCount, decimal Invoiced, decimal Paid, decimal Outstanding, string Currency)> GetInvoiceTotalsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DuplicateGroupRow>> FindDuplicatesAsync(DuplicateKeyKind key, CancellationToken cancellationToken = default);
}

public interface ICustomerAddressRepository
{
    Task<CustomerAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerAddress>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default);
    void Update(CustomerAddress address);
    void Remove(CustomerAddress address);
    Task ClearPrimaryAsync(Guid customerId, Guid? excludeAddressId, CancellationToken cancellationToken = default);
}

public interface ICustomerContactRepository
{
    Task<CustomerContact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerContact>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerContact contact, CancellationToken cancellationToken = default);
    void Update(CustomerContact contact);
    void Remove(CustomerContact contact);
    Task ClearPrimaryAsync(Guid customerId, Guid? excludeContactId, CancellationToken cancellationToken = default);
}

public interface ICustomerTransactionRepository
{
    Task AddAsync(CustomerTransaction transaction, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CustomerTransaction> Items, int Total)> GetByCustomerAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
}
