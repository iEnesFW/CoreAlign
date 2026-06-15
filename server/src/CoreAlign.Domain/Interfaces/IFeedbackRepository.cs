using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IFeedbackRepository
{
    Task<FeedbackTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeedbackTicket>> ListAsync(FeedbackStatus? status, FeedbackType? type, CancellationToken cancellationToken = default);
    Task AddAsync(FeedbackTicket ticket, CancellationToken cancellationToken = default);
    void Update(FeedbackTicket ticket);
}
