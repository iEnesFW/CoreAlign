using CoreAlign.Application.Tax.Commands;
using FluentValidation;

namespace CoreAlign.Application.Tax.Validators;

public class BuildKdv1ForPeriodCommandValidator : AbstractValidator<BuildKdv1ForPeriodCommand>
{
    public BuildKdv1ForPeriodCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class BuildBaBsForPeriodCommandValidator : AbstractValidator<BuildBaBsForPeriodCommand>
{
    public BuildBaBsForPeriodCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class MarkTaxDeclarationRejectedCommandValidator
    : AbstractValidator<MarkTaxDeclarationRejectedCommand>
{
    public MarkTaxDeclarationRejectedCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

public class MarkTaxDeclarationSubmittedCommandValidator
    : AbstractValidator<MarkTaxDeclarationSubmittedCommand>
{
    public MarkTaxDeclarationSubmittedCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class MarkTaxDeclarationAcceptedCommandValidator
    : AbstractValidator<MarkTaxDeclarationAcceptedCommand>
{
    public MarkTaxDeclarationAcceptedCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
