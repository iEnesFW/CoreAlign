using FluentValidation;

namespace CoreAlign.Application.B2B.DealerPortal;

public class PostDealerPortalOrderCommentCommandValidator : AbstractValidator<PostDealerPortalOrderCommentCommand>
{
    public PostDealerPortalOrderCommentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEqual(Guid.Empty);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(1).MaximumLength(4000);
    }
}
