using CoreAlign.Application.Accounting.Commands;
using FluentValidation;

namespace CoreAlign.Application.Accounting.Validators;

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Validation.Required")
            .Must(t => Enum.TryParse<Domain.Enums.JournalEntryType>(t, ignoreCase: true, out _))
            .WithMessage("Validation.InvalidJournalEntryType");
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.Reference).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Reference));
        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Validation.Required")
            .Must(l => l != null && l.Count >= 2).WithMessage("Validation.JournalLinesAtLeastTwo");
        RuleForEach(x => x.Lines).SetValidator(new JournalLineInputValidator());
    }
}

public class UpdateJournalEntryHeaderCommandValidator : AbstractValidator<UpdateJournalEntryHeaderCommand>
{
    public UpdateJournalEntryHeaderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Validation.Required")
            .Must(t => Enum.TryParse<Domain.Enums.JournalEntryType>(t, ignoreCase: true, out _))
            .WithMessage("Validation.InvalidJournalEntryType");
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.Reference).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Reference));
    }
}

public class ReplaceJournalEntryLinesCommandValidator : AbstractValidator<ReplaceJournalEntryLinesCommand>
{
    public ReplaceJournalEntryLinesCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Validation.Required")
            .Must(l => l != null && l.Count >= 2).WithMessage("Validation.JournalLinesAtLeastTwo");
        RuleForEach(x => x.Lines).SetValidator(new JournalLineInputValidator());
    }
}

public class PostJournalEntryCommandValidator : AbstractValidator<PostJournalEntryCommand>
{
    public PostJournalEntryCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}

public class ReverseJournalEntryCommandValidator : AbstractValidator<ReverseJournalEntryCommand>
{
    public ReverseJournalEntryCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}

public class DeleteJournalEntryCommandValidator : AbstractValidator<DeleteJournalEntryCommand>
{
    public DeleteJournalEntryCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}

public class JournalLineInputValidator : AbstractValidator<JournalLineInput>
{
    public JournalLineInputValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Validation.Required");
        RuleFor(x => x.Debit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Credit).GreaterThanOrEqualTo(0);
        RuleFor(x => x)
            .Must(l => (l.Debit > 0 && l.Credit == 0) || (l.Debit == 0 && l.Credit > 0))
            .WithMessage("Validation.JournalLineSides");
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Validation.Required")
            .Length(3).WithMessage("Validation.CurrencyMustBeIso");
        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.CostCenter).MaximumLength(64).When(x => !string.IsNullOrEmpty(x.CostCenter));
        RuleFor(x => x.Project).MaximumLength(64).When(x => !string.IsNullOrEmpty(x.Project));
    }
}
