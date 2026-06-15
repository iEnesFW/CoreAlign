using CoreAlign.Application.B2B.PortalComments;
using CoreAlign.Application.Collaboration;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public class ListDealerPortalOrderCommentsHandler
    : IRequestHandler<ListDealerPortalOrderCommentsQuery, IReadOnlyList<CommentDto>>
{
    private const string EntityType = "Order";

    private readonly IPortalScopeService _scope;
    private readonly IOrderRepository _orders;
    private readonly ICommentRepository _comments;
    private readonly IUserRepository _users;

    public ListDealerPortalOrderCommentsHandler(
        IPortalScopeService scope,
        IOrderRepository orders,
        ICommentRepository comments,
        IUserRepository users)
    {
        _scope = scope;
        _orders = orders;
        _comments = comments;
        _users = users;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(ListDealerPortalOrderCommentsQuery request, CancellationToken cancellationToken)
    {
        var dealerAccountId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.OriginDealerAccountId != dealerAccountId)
        {
            throw new OrderNotFoundException();
        }

        var rows = await _comments.ListByEntityAsync(EntityType, request.OrderId, cancellationToken);
        if (rows.Count == 0) return Array.Empty<CommentDto>();

        var authorIds = rows.Select(r => r.AuthorUserId).Distinct().ToList();
        var lookup = await BuildAuthorLookupAsync(authorIds, cancellationToken);
        return rows.Select(r => PortalCommentMapper.ToDto(r, lookup)).ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, User>> BuildAuthorLookupAsync(IReadOnlyList<Guid> authorIds, CancellationToken cancellationToken)
    {
        var dictionary = new Dictionary<Guid, User>(authorIds.Count);
        foreach (var id in authorIds)
        {
            var user = await _users.GetByIdAsync(id, cancellationToken);
            if (user is not null) dictionary[user.Id] = user;
        }
        return dictionary;
    }
}

public class PostDealerPortalOrderCommentHandler
    : IRequestHandler<PostDealerPortalOrderCommentCommand, CommentDto>
{
    private const string EntityType = "Order";

    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IOrderRepository _orders;
    private readonly ICommentRepository _comments;
    private readonly IUserRepository _users;
    private readonly IOrderCommentPostedOutbox _outbox;
    private readonly IUnitOfWork _uow;

    public PostDealerPortalOrderCommentHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IOrderRepository orders,
        ICommentRepository comments,
        IUserRepository users,
        IOrderCommentPostedOutbox outbox,
        IUnitOfWork uow)
    {
        _scope = scope;
        _currentUser = currentUser;
        _orders = orders;
        _comments = comments;
        _users = users;
        _outbox = outbox;
        _uow = uow;
    }

    public async Task<CommentDto> Handle(PostDealerPortalOrderCommentCommand request, CancellationToken cancellationToken)
    {
        var dealerAccountId = await _scope.GetCurrentDealerAccountIdAsync(cancellationToken);
        var authorId = _currentUser.UserIdOrThrow();

        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null || order.OriginDealerAccountId != dealerAccountId)
        {
            throw new OrderNotFoundException();
        }

        var comment = new Comment(EntityType, request.OrderId, authorId, request.Body);
        await _comments.AddAsync(comment, cancellationToken);

        await _outbox.EnqueueAsync(new OrderCommentPostedPayload(
            OrderId: order.Id,
            CommentId: comment.Id,
            AuthorUserId: authorId,
            AuthorPersona: "dealer",
            Excerpt: PortalCommentMapper.BuildExcerpt(comment.Body),
            OriginDealerAccountId: order.OriginDealerAccountId,
            CustomerId: order.CustomerId), cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        var author = await _users.GetByIdAsync(authorId, cancellationToken);
        var lookup = author is null ? new Dictionary<Guid, User>() : new Dictionary<Guid, User> { [author.Id] = author };
        return PortalCommentMapper.ToDto(comment, lookup);
    }
}
