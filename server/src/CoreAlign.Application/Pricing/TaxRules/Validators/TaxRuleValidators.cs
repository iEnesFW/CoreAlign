using CoreAlign.Application.Pricing.TaxRules.Commands;
using CoreAlign.Domain.Entities.Pricing;
using FluentValidation;

namespace CoreAlign.Application.Pricing.TaxRules.Validators;

public class CreateTaxRuleCommandValidator : AbstractValidator<CreateTaxRuleCommand>
{
    public CreateTaxRuleCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m).WithMessage("Validation.TaxRateRange");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.RegionCode).MaximumLength(32);
        RuleFor(x => x.ProductClass).MaximumLength(64);
        RuleFor(x => x).Must(c => !(c.ValidFromUtc.HasValue && c.ValidUntilUtc.HasValue)
                || c.ValidUntilUtc!.Value >= c.ValidFromUtc!.Value)
            .WithMessage("Validation.ValidUntilMustBeAfterValidFrom");
        RuleFor(x => x.RegionCode).NotEmpty()
            .When(x => x.Scope == TaxRuleScope.Region || x.Scope == TaxRuleScope.RegionAndProductClass)
            .WithMessage("Validation.RegionCodeRequired");
        RuleFor(x => x).Must(c => !string.IsNullOrWhiteSpace(c.ProductClass) || c.ProductCategoryId.HasValue)
            .When(c => c.Scope == TaxRuleScope.ProductClass || c.Scope == TaxRuleScope.RegionAndProductClass)
            .WithMessage("Validation.ProductClassOrCategoryRequired");
        RuleFor(x => x.ProductId).NotEmpty()
            .When(x => x.Scope == TaxRuleScope.Product)
            .WithMessage("Validation.ProductRequired");
    }
}

public class UpdateTaxRuleCommandValidator : AbstractValidator<UpdateTaxRuleCommand>
{
    public UpdateTaxRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RatePercent).InclusiveBetween(0m, 100m).WithMessage("Validation.TaxRateRange");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.RegionCode).MaximumLength(32);
        RuleFor(x => x.ProductClass).MaximumLength(64);
        RuleFor(x => x).Must(c => !(c.ValidFromUtc.HasValue && c.ValidUntilUtc.HasValue)
                || c.ValidUntilUtc!.Value >= c.ValidFromUtc!.Value)
            .WithMessage("Validation.ValidUntilMustBeAfterValidFrom");
        RuleFor(x => x.RegionCode).NotEmpty()
            .When(x => x.Scope == TaxRuleScope.Region || x.Scope == TaxRuleScope.RegionAndProductClass)
            .WithMessage("Validation.RegionCodeRequired");
        RuleFor(x => x).Must(c => !string.IsNullOrWhiteSpace(c.ProductClass) || c.ProductCategoryId.HasValue)
            .When(c => c.Scope == TaxRuleScope.ProductClass || c.Scope == TaxRuleScope.RegionAndProductClass)
            .WithMessage("Validation.ProductClassOrCategoryRequired");
        RuleFor(x => x.ProductId).NotEmpty()
            .When(x => x.Scope == TaxRuleScope.Product)
            .WithMessage("Validation.ProductRequired");
    }
}

public class DeleteTaxRuleCommandValidator : AbstractValidator<DeleteTaxRuleCommand>
{
    public DeleteTaxRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
