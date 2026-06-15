using FluentValidation;

namespace CoreAlign.Application.Orders.Revisions;

public class RequestOrderRevisionCommandValidator : AbstractValidator<RequestOrderRevisionCommand>
{
    public RequestOrderRevisionCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ProposedLines)
            .NotNull()
            .Must(l => l != null && l.Count > 0)
            .WithMessage("A revision must contain at least one line.");
        RuleForEach(x => x.ProposedLines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0m);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0m);
        });
    }
}

public class ApproveOrderRevisionCommandValidator : AbstractValidator<ApproveOrderRevisionCommand>
{
    public ApproveOrderRevisionCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RevisionId).NotEmpty();
    }
}

public class RejectOrderRevisionCommandValidator : AbstractValidator<RejectOrderRevisionCommand>
{
    public RejectOrderRevisionCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RevisionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class CancelOrderRevisionCommandValidator : AbstractValidator<CancelOrderRevisionCommand>
{
    public CancelOrderRevisionCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.RevisionId).NotEmpty();
    }
}
