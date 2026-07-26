using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IFeedbackCommentRepository
{
    Task<IReadOnlyList<FeedbackTicketComment>> ListByTicketAsync(
        Guid ticketId,
        bool includeInternal,
        CancellationToken cancellationToken = default);
    Task AddAsync(FeedbackTicketComment comment, CancellationToken cancellationToken = default);
}

public interface IFeedbackAttachmentRepository
{
    Task<IReadOnlyList<FeedbackAttachment>> ListByTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);
    Task<FeedbackAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task AddAsync(FeedbackAttachment attachment, CancellationToken cancellationToken = default);
    void Remove(FeedbackAttachment attachment);
}
