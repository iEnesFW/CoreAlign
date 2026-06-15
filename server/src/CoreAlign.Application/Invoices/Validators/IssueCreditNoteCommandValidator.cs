using CoreAlign.Application.Invoices.Commands;
using FluentValidation;

namespace CoreAlign.Application.Invoices.Validators;

public class IssueCreditNoteCommandValidator : AbstractValidator<IssueCreditNoteCommand>
{
    public IssueCreditNoteCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.AtLeastOneLine");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.InvoiceLineId).NotEmpty();
            line.RuleFor(l => l.Quantity)
                .GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
        });
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
