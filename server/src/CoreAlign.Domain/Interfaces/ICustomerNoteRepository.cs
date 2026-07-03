using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface ICustomerNoteRepository
{
    Task AddAsync(CustomerNote note, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerNote>> GetLatestByCustomerAsync(Guid customerId, int take, CancellationToken cancellationToken = default);
}
