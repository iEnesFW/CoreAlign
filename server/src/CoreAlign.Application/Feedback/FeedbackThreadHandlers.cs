using CoreAlign.Application.Feedback.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Feedback;

public static class FeedbackAttachmentPolicy
{
    public const int MaxPerTicket = 5;
}

internal static class FeedbackThreadMapper
{
    public static FeedbackCommentDto ToDto(FeedbackTicketComment c) => new(
        c.Id,
        c.FeedbackTicketId,
        c.AuthorUserId,
        c.AuthorName,
        c.Body,
        c.IsInternal,
        c.CreatedAtUtc);

    public static FeedbackAttachmentDto ToDto(FeedbackAttachment a) => new(
        a.Id,
        a.FeedbackTicketId,
        a.DisplayFileName,
        a.ContentType,
        a.SizeBytes,
        a.CreatedAtUtc);
}

public class AddFeedbackCommentHandler : IRequestHandler<AddFeedbackCommentCommand, FeedbackCommentDto>
{
    private readonly IFeedbackRepository _tickets;
    private readonly IFeedbackCommentRepository _comments;
    private readonly IFeedbackNotificationOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public AddFeedbackCommentHandler(
        IFeedbackRepository tickets,
        IFeedbackCommentRepository comments,
        IFeedbackNotificationOutbox outbox,
        IUnitOfWork uow)
    {
        _tickets = tickets;
        _comments = comments;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<FeedbackCommentDto> Handle(AddFeedbackCommentCommand c, CancellationToken ct)
    {
        var ticket = await _tickets.GetByIdAsync(c.TicketId, ct) ?? throw new FeedbackNotFoundException();
        if (c.IsInternal && !c.IsPlatformAdmin)
        {
            throw new FeedbackCommentForbiddenException();
        }
        var comment = new FeedbackTicketComment(
            ticket.Id,
            c.Body,
            c.AuthorUserId,
            string.IsNullOrWhiteSpace(c.AuthorName) ? null : c.AuthorName.Trim(),
            c.IsInternal);
        await _comments.AddAsync(comment, ct);
        // An internal note is invisible to the reporter, so telling them one exists would leak it.
        if (!c.IsInternal && ticket.CreatedByUserId.HasValue && ticket.CreatedByUserId != c.AuthorUserId)
        {
            await _outbox.EnqueueAsync(FeedbackNotificationPayload.CommentAdded(ticket, comment), ct);
        }
        await _uow.SaveChangesAsync(ct);
        return FeedbackThreadMapper.ToDto(comment);
    }
}

public class ListFeedbackCommentsHandler
    : IRequestHandler<ListFeedbackCommentsQuery, IReadOnlyList<FeedbackCommentDto>>
{
    private readonly IFeedbackRepository _tickets;
    private readonly IFeedbackCommentRepository _comments;

    public ListFeedbackCommentsHandler(IFeedbackRepository tickets, IFeedbackCommentRepository comments)
    {
        _tickets = tickets;
        _comments = comments;
    }

    public async Task<IReadOnlyList<FeedbackCommentDto>> Handle(
        ListFeedbackCommentsQuery q,
        CancellationToken ct)
    {
        _ = await _tickets.GetByIdAsync(q.TicketId, ct) ?? throw new FeedbackNotFoundException();
        var comments = await _comments.ListByTicketAsync(q.TicketId, q.IncludeInternal, ct);
        return comments.Select(FeedbackThreadMapper.ToDto).ToList();
    }
}

public class AddFeedbackAttachmentsHandler
    : IRequestHandler<AddFeedbackAttachmentsCommand, IReadOnlyList<FeedbackAttachmentDto>>
{
    private readonly IFeedbackRepository _tickets;
    private readonly IFeedbackAttachmentRepository _attachments;
    private readonly IUnitOfWork _uow;

    public AddFeedbackAttachmentsHandler(
        IFeedbackRepository tickets,
        IFeedbackAttachmentRepository attachments,
        IUnitOfWork uow)
    {
        _tickets = tickets;
        _attachments = attachments;
        _uow = uow;
    }

    public async Task<IReadOnlyList<FeedbackAttachmentDto>> Handle(
        AddFeedbackAttachmentsCommand c,
        CancellationToken ct)
    {
        var ticket = await _tickets.GetByIdAsync(c.TicketId, ct) ?? throw new FeedbackNotFoundException();
        var existing = await _attachments.CountByTicketAsync(ticket.Id, ct);
        if (existing + c.Files.Count > FeedbackAttachmentPolicy.MaxPerTicket)
        {
            throw new FeedbackAttachmentLimitExceededException(FeedbackAttachmentPolicy.MaxPerTicket);
        }
        var created = new List<FeedbackAttachment>(c.Files.Count);
        var order = existing;
        foreach (var file in c.Files)
        {
            var attachment = new FeedbackAttachment(
                ticket.Id,
                file.RelativePath,
                file.DisplayFileName,
                file.ContentType,
                file.SizeBytes,
                c.UploadedByUserId,
                order);
            await _attachments.AddAsync(attachment, ct);
            created.Add(attachment);
            order += 1;
        }
        // Mirror the first attachment into the legacy single-attachment columns so the older
        // GET /feedback/{id}/attachment endpoint keeps serving something for new tickets too.
        if (existing == 0 && created.Count > 0 && ticket.AttachmentPath is null)
        {
            var first = created[0];
            ticket.AttachFile(first.StoragePath, first.DisplayFileName, first.ContentType);
            _tickets.Update(ticket);
        }
        await _uow.SaveChangesAsync(ct);
        return created.Select(FeedbackThreadMapper.ToDto).ToList();
    }
}

public class ListFeedbackAttachmentsHandler
    : IRequestHandler<ListFeedbackAttachmentsQuery, IReadOnlyList<FeedbackAttachmentDto>>
{
    private readonly IFeedbackRepository _tickets;
    private readonly IFeedbackAttachmentRepository _attachments;

    public ListFeedbackAttachmentsHandler(
        IFeedbackRepository tickets,
        IFeedbackAttachmentRepository attachments)
    {
        _tickets = tickets;
        _attachments = attachments;
    }

    public async Task<IReadOnlyList<FeedbackAttachmentDto>> Handle(
        ListFeedbackAttachmentsQuery q,
        CancellationToken ct)
    {
        _ = await _tickets.GetByIdAsync(q.TicketId, ct) ?? throw new FeedbackNotFoundException();
        var items = await _attachments.ListByTicketAsync(q.TicketId, ct);
        return items.Select(FeedbackThreadMapper.ToDto).ToList();
    }
}

public class GetFeedbackAttachmentFileHandler
    : IRequestHandler<GetFeedbackAttachmentFileQuery, FeedbackAttachmentDescriptor?>
{
    private readonly IFeedbackAttachmentRepository _attachments;

    public GetFeedbackAttachmentFileHandler(IFeedbackAttachmentRepository attachments) =>
        _attachments = attachments;

    public async Task<FeedbackAttachmentDescriptor?> Handle(
        GetFeedbackAttachmentFileQuery q,
        CancellationToken ct)
    {
        var attachment = await _attachments.GetByIdAsync(q.AttachmentId, ct);
        // WHY: the route ticket must own the attachment, otherwise a valid attachment id pointed at
        // the wrong ticket would stream a different ticket's file.
        if (attachment is null || attachment.FeedbackTicketId != q.TicketId)
        {
            return null;
        }
        return new FeedbackAttachmentDescriptor(
            attachment.StoragePath,
            attachment.DisplayFileName,
            attachment.ContentType);
    }
}

public class DeleteFeedbackAttachmentHandler : IRequestHandler<DeleteFeedbackAttachmentCommand, Unit>
{
    private readonly IFeedbackAttachmentRepository _attachments;
    private readonly IUnitOfWork _uow;

    public DeleteFeedbackAttachmentHandler(IFeedbackAttachmentRepository attachments, IUnitOfWork uow)
    {
        _attachments = attachments;
        _uow = uow;
    }

    public async Task<Unit> Handle(DeleteFeedbackAttachmentCommand c, CancellationToken ct)
    {
        var attachment = await _attachments.GetByIdAsync(c.AttachmentId, ct);
        if (attachment is null || attachment.FeedbackTicketId != c.TicketId)
        {
            throw new FeedbackAttachmentNotFoundException();
        }
        _attachments.Remove(attachment);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
