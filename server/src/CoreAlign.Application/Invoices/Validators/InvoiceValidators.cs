using CoreAlign.Application.Invoices.Commands;
using FluentValidation;

namespace CoreAlign.Application.Invoices.Validators;

public class GenerateInvoiceFromOrderCommandValidator : AbstractValidator<GenerateInvoiceFromOrderCommand>
{
    public GenerateInvoiceFromOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DueDays).InclusiveBetween(0, 365);
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
