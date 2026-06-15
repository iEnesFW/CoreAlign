using FluentValidation;

namespace CoreAlign.Application.B2B.CustomerPortal;

public class PostCustomerPortalOrderCommentCommandValidator : AbstractValidator<PostCustomerPortalOrderCommentCommand>
{
    public PostCustomerPortalOrderCommentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEqual(Guid.Empty);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(1).MaximumLength(4000);
    }
}
