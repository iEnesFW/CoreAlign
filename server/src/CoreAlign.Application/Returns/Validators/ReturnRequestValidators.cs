using CoreAlign.Application.Returns.Commands;
using FluentValidation;

namespace CoreAlign.Application.Returns.Validators;

public class CreateReturnRequestCommandValidator : AbstractValidator<CreateReturnRequestCommand>
{
    public CreateReturnRequestCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.AtLeastOneLine");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.OrderLineId).NotEmpty();
            line.RuleFor(l => l.QuantityReturned)
                .GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
            line.RuleFor(l => l.LineNotes)
                .MaximumLength(500).WithMessage("Validation.TooLong")
                .When(l => !string.IsNullOrEmpty(l.LineNotes));
        });
        RuleFor(x => x.ReasonText)
            .MaximumLength(500).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.ReasonText));
        RuleFor(x => x.CustomerNotes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.CustomerNotes));
        RuleFor(x => x.InternalNotes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.InternalNotes));
    }
}

public class ApproveReturnRequestCommandValidator : AbstractValidator<ApproveReturnRequestCommand>
{
    public ApproveReturnRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class RejectReturnRequestCommandValidator : AbstractValidator<RejectReturnRequestCommand>
{
    public RejectReturnRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

public class CancelReturnRequestCommandValidator : AbstractValidator<CancelReturnRequestCommand>
{
    public CancelReturnRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ReceiveReturnedItemsCommandValidator : AbstractValidator<ReceiveReturnedItemsCommand>
{
    public ReceiveReturnedItemsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("Validation.WarehouseRequired");
    }
}
