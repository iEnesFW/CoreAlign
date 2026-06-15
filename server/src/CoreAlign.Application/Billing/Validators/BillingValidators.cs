using System.Text.RegularExpressions;
using FluentValidation;

namespace CoreAlign.Application.Billing.Validators;

public partial class CreateSubscriptionOrderCommandValidator : AbstractValidator<CreateSubscriptionOrderCommand>
{
    public const string MockGatewayName = "mock";

    public CreateSubscriptionOrderCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ModuleId).NotEmpty();
            item.RuleFor(i => i.PlanId).NotEmpty();
        });
        RuleFor(x => x.GatewayName).MaximumLength(32);

        When(x => RequiresBillingInfo(x.GatewayName), () =>
        {
            RuleFor(x => x.BillingInfo)
                .NotNull()
                .WithMessage("Validation.BillingInfoRequired");

            When(x => x.BillingInfo is not null, () =>
            {
                RuleFor(x => x.BillingInfo!.Name)
                    .NotEmpty().WithMessage("Validation.BillingNameRequired")
                    .MaximumLength(100);
                RuleFor(x => x.BillingInfo!.Surname)
                    .NotEmpty().WithMessage("Validation.BillingSurnameRequired")
                    .MaximumLength(100);
                RuleFor(x => x.BillingInfo!.Email)
                    .NotEmpty().WithMessage("Validation.BillingEmailRequired")
                    .EmailAddress().WithMessage("Validation.BillingEmailInvalid")
                    .MaximumLength(256);
                RuleFor(x => x.BillingInfo!.GsmNumber)
                    .NotEmpty().WithMessage("Validation.BillingGsmRequired")
                    .Must(BeValidGsm).WithMessage("Validation.BillingGsmInvalid")
                    .MaximumLength(32);
                RuleFor(x => x.BillingInfo!.IdentityNumber)
                    .NotEmpty().WithMessage("Validation.BillingIdentityRequired")
                    .Must(BeValidIdentity).WithMessage("Validation.BillingIdentityInvalid")
                    .MaximumLength(32);
                RuleFor(x => x.BillingInfo!.Address)
                    .NotEmpty().WithMessage("Validation.BillingAddressRequired")
                    .MaximumLength(500);
                RuleFor(x => x.BillingInfo!.City)
                    .NotEmpty().WithMessage("Validation.BillingCityRequired")
                    .MaximumLength(100);
                RuleFor(x => x.BillingInfo!.Country)
                    .NotEmpty().WithMessage("Validation.BillingCountryRequired")
                    .Must(c => !string.IsNullOrWhiteSpace(c) && c.Trim().Length >= 2)
                    .WithMessage("Validation.BillingCountryInvalid")
                    .MaximumLength(100);
                RuleFor(x => x.BillingInfo!.ZipCode)
                    .NotEmpty().WithMessage("Validation.BillingZipRequired")
                    .MaximumLength(32);
            });
        });
    }

    public static bool RequiresBillingInfo(string? gatewayName)
    {
        if (string.IsNullOrWhiteSpace(gatewayName)) return false;
        return !string.Equals(gatewayName.Trim(), MockGatewayName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool BeValidGsm(string? gsm)
    {
        if (string.IsNullOrWhiteSpace(gsm)) return false;
        var trimmed = gsm.Trim();
        return GsmRegex().IsMatch(trimmed);
    }

    private static bool BeValidIdentity(string? identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return false;
        var trimmed = identity.Trim();
        return trimmed.Length >= 5 && trimmed.Length <= 32 && IdentityRegex().IsMatch(trimmed);
    }

    [GeneratedRegex(@"^\+?[0-9]{10,15}$")]
    private static partial Regex GsmRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]+$")]
    private static partial Regex IdentityRegex();
}

public class ApplyMockPaymentApprovalCommandValidator : AbstractValidator<ApplyMockPaymentApprovalCommand>
{
    public ApplyMockPaymentApprovalCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Action)
            .NotEmpty()
            .Must(a => a is "approve" or "cancel" or "fail")
            .WithMessage("Action must be one of: approve, cancel, fail.");
    }
}

public class CancelSubscriptionOrderCommandValidator : AbstractValidator<CancelSubscriptionOrderCommand>
{
    public CancelSubscriptionOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    public ProcessPaymentWebhookCommandValidator()
    {
        RuleFor(x => x.GatewayName).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Payload).NotEmpty();
    }
}
