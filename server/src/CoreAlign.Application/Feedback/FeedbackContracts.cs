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
    string? AttachmentFileName,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    Guid? CreatedByUserId,
    int StatusChangeCount,
    IReadOnlyList<FeedbackStatus> AllowedNextStatuses);

public record FeedbackAttachmentDescriptor(string RelativePath, string FileName, string ContentType);

public record CreateFeedbackCommand(
    FeedbackType Type,
    string Title,
    string Description,
    FeedbackPriority Priority,
    string? Module = null,
    string? StepsToReproduce = null,
    string? PageUrl = null,
    string? CreatedByName = null,
    Guid? CreatedByUserId = null) : IRequest<FeedbackTicketDto>, ITransactionalRequest;

public record UpdateFeedbackStatusCommand(
    Guid Id,
    FeedbackStatus Status,
    string? AdminResponse) : IRequest<FeedbackTicketDto>, ITransactionalRequest;

public record ListFeedbackQuery(FeedbackStatus? Status, FeedbackType? Type)
    : IRequest<IReadOnlyList<FeedbackTicketDto>>;

public record GetFeedbackByIdQuery(Guid Id) : IRequest<FeedbackTicketDto?>;

public record AttachFeedbackFileCommand(Guid Id, string RelativePath, string FileName, string ContentType)
    : IRequest<FeedbackTicketDto>, ITransactionalRequest;

public record GetFeedbackAttachmentQuery(Guid Id) : IRequest<FeedbackAttachmentDescriptor?>;

public record FeedbackCommentDto(
    Guid Id,
    Guid TicketId,
    Guid? AuthorUserId,
    string? AuthorName,
    string Body,
    bool IsInternal,
    DateTime CreatedAtUtc);

public record FeedbackAttachmentDto(
    Guid Id,
    Guid TicketId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAtUtc);

public record FeedbackUploadedFile(
    string RelativePath,
    string DisplayFileName,
    string ContentType,
    long SizeBytes);

public record AddFeedbackCommentCommand(
    Guid TicketId,
    string Body,
    Guid? AuthorUserId,
    string? AuthorName,
    bool IsInternal,
    bool IsPlatformAdmin) : IRequest<FeedbackCommentDto>, ITransactionalRequest;

public record ListFeedbackCommentsQuery(Guid TicketId, bool IncludeInternal)
    : IRequest<IReadOnlyList<FeedbackCommentDto>>;

public record AddFeedbackAttachmentsCommand(
    Guid TicketId,
    IReadOnlyList<FeedbackUploadedFile> Files,
    Guid? UploadedByUserId) : IRequest<IReadOnlyList<FeedbackAttachmentDto>>, ITransactionalRequest;

public record ListFeedbackAttachmentsQuery(Guid TicketId)
    : IRequest<IReadOnlyList<FeedbackAttachmentDto>>;

public record GetFeedbackAttachmentFileQuery(Guid TicketId, Guid AttachmentId)
    : IRequest<FeedbackAttachmentDescriptor?>;

public record DeleteFeedbackAttachmentCommand(Guid TicketId, Guid AttachmentId)
    : IRequest<Unit>, ITransactionalRequest;
