using FluentValidation;

namespace CoreAlign.Application.Customers.Merge;

public sealed class MergeCustomersCommandValidator : AbstractValidator<MergeCustomersCommand>
{
    public MergeCustomersCommandValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEqual(Guid.Empty).WithMessage("Validation.Required");

        RuleFor(x => x.SourceCustomerId)
            .NotEqual(Guid.Empty).WithMessage("Validation.Required");

        RuleFor(x => x.TargetCustomerId)
            .NotEqual(Guid.Empty).WithMessage("Validation.Required");

        RuleFor(x => x)
            .Must(x => x.SourceCustomerId != x.TargetCustomerId)
            .WithMessage("Customers.Merge.SourceTargetMustDiffer");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
