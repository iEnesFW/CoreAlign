using CoreAlign.Application.Common;
using CoreAlign.Domain.Enums;
using MediatR;

namespace CoreAlign.Application.Feedback;

public record FeedbackTicketDto(
    Guid Id,
    FeedbackType Type,
    string Title,
    string Description,
    FeedbackPriority Priority,
    FeedbackStatus Status,
    string? Module,
    string? StepsToReproduce,
    string? PageUrl,
    string? CreatedByName,
    string? AdminResponse,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);

public record CreateFeedbackCommand(
    FeedbackType Type,
    string Title,
    string Description,
    FeedbackPriority Priority,
    string? Module = null,
    string? StepsToReproduce = null,
    string? PageUrl = null,
    string? CreatedByName = null) : IRequest<FeedbackTicketDto>, ITransactionalRequest;

public record UpdateFeedbackStatusCommand(
    Guid Id,
    FeedbackStatus Status,
    string? AdminResponse) : IRequest<FeedbackTicketDto>, ITransactionalRequest;

public record ListFeedbackQuery(FeedbackStatus? Status, FeedbackType? Type)
    : IRequest<IReadOnlyList<FeedbackTicketDto>>;

public record GetFeedbackByIdQuery(Guid Id) : IRequest<FeedbackTicketDto?>;
