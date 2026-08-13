using CoreAlign.Application.Invoices.Recurring.Commands;
using CoreAlign.Application.Invoices.Recurring.DTOs;
using CoreAlign.Application.Treasury.Fx;
using FluentValidation;

namespace CoreAlign.Application.Invoices.Recurring.Validators;

public class CreateRecurringInvoiceTemplateCommandValidator : AbstractValidator<CreateRecurringInvoiceTemplateCommand>
{
    public CreateRecurringInvoiceTemplateCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3).MustBeAKnownCurrency(currencyGuard);
        RuleFor(x => x.IntervalCount).GreaterThanOrEqualTo(1);
        RuleFor(x => x.AnchorDayOfMonth).InclusiveBetween(1, 31).When(x => x.AnchorDayOfMonth.HasValue);
        RuleFor(x => x.MaxOccurrences).GreaterThanOrEqualTo(1).When(x => x.MaxOccurrences.HasValue);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.DueDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Validation.RecurringInvoiceNoLines");
        RuleForEach(x => x.Lines).SetValidator(new RecurringInvoiceLineInputValidator());
    }
}

public class UpdateRecurringInvoiceTemplateCommandValidator : AbstractValidator<UpdateRecurringInvoiceTemplateCommand>
{
    public UpdateRecurringInvoiceTemplateCommandValidator(IKnownCurrencyGuard currencyGuard)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3).MustBeAKnownCurrency(currencyGuard);
        RuleFor(x => x.IntervalCount).GreaterThanOrEqualTo(1);
        RuleFor(x => x.AnchorDayOfMonth).InclusiveBetween(1, 31).When(x => x.AnchorDayOfMonth.HasValue);
        RuleFor(x => x.MaxOccurrences).GreaterThanOrEqualTo(1).When(x => x.MaxOccurrences.HasValue);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.DueDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).SetValidator(new RecurringInvoiceLineInputValidator());
    }
}

public class RecurringInvoiceLineInputValidator : AbstractValidator<RecurringInvoiceLineInput>
{
    public RecurringInvoiceLineInputValidator()
    {
        RuleFor(l => l.Quantity).GreaterThan(0m).WithMessage("Validation.QuantityMustBePositive");
        RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(l => l.TaxRatePercent).InclusiveBetween(0m, 100m);
        RuleFor(l => l).Must(l => l.ProductId.HasValue || !string.IsNullOrWhiteSpace(l.ProductName))
            .WithMessage("Validation.RecurringInvoiceLineNeedsProductOrName");
    }
}

public class PauseRecurringInvoiceTemplateCommandValidator : AbstractValidator<PauseRecurringInvoiceTemplateCommand>
{
    public PauseRecurringInvoiceTemplateCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class ResumeRecurringInvoiceTemplateCommandValidator : AbstractValidator<ResumeRecurringInvoiceTemplateCommand>
{
    public ResumeRecurringInvoiceTemplateCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class CancelRecurringInvoiceTemplateCommandValidator : AbstractValidator<CancelRecurringInvoiceTemplateCommand>
{
    public CancelRecurringInvoiceTemplateCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class RunRecurringInvoiceNowCommandValidator : AbstractValidator<RunRecurringInvoiceNowCommand>
{
    public RunRecurringInvoiceNowCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
