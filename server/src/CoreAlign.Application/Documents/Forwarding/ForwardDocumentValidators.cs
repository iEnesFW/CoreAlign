using FluentValidation;

namespace CoreAlign.Application.Documents.Forwarding;

public sealed class ForwardCustomerDocumentCommandValidator : AbstractValidator<ForwardCustomerDocumentCommand>
{
    public ForwardCustomerDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.DocumentId).NotEqual(Guid.Empty);
        RuleFor(x => x.RecipientEmail).ApplyRecipientRules();
    }
}

public sealed class ForwardDealerDocumentCommandValidator : AbstractValidator<ForwardDealerDocumentCommand>
{
    public ForwardDealerDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.DocumentId).NotEqual(Guid.Empty);
        RuleFor(x => x.RecipientEmail).ApplyRecipientRules();
    }
}

internal static class ForwardValidationRules
{
    public static IRuleBuilderOptions<T, string> ApplyRecipientRules<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254)
            .Must(NoHeaderInjection).WithMessage("Recipient address contains invalid characters.");

    private static bool NoHeaderInjection(string value) =>
        value is not null && !value.Any(c => c is '\r' or '\n' or ';' or ',');
}
