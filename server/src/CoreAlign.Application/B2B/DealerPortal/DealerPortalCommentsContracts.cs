using CoreAlign.Application.Collaboration;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.B2B.DealerPortal;

public record ListDealerPortalOrderCommentsQuery(Guid OrderId)
    : IRequest<IReadOnlyList<CommentDto>>;

public record PostDealerPortalOrderCommentCommand(Guid OrderId, string Body)
    : IRequest<CommentDto>, ITransactionalRequest;
