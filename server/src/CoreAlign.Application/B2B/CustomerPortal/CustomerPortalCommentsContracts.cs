using CoreAlign.Application.Collaboration;
using CoreAlign.Application.Common;
using MediatR;

namespace CoreAlign.Application.B2B.CustomerPortal;

public record ListCustomerPortalOrderCommentsQuery(Guid OrderId)
    : IRequest<IReadOnlyList<CommentDto>>;

public record PostCustomerPortalOrderCommentCommand(Guid OrderId, string Body)
    : IRequest<CommentDto>, ITransactionalRequest;
