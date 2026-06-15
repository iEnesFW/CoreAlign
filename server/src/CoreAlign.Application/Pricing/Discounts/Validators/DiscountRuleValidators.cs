using CoreAlign.Application.Pricing.Discounts.Commands;
using CoreAlign.Domain.Entities.Pricing;
using FluentValidation;

namespace CoreAlign.Application.Pricing.Discounts.Validators;

public class CreateDiscountRuleCommandValidator : AbstractValidator<CreateDiscountRuleCommand>
{
    public CreateDiscountRuleCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0m).WithMessage("Validation.DiscountValueMustBeNonNegative");
        RuleFor(x => x.Value).LessThanOrEqualTo(100m)
            .When(x => x.ValueType == DiscountValueType.Percent)
            .WithMessage("Validation.DiscountPercentRange");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x).Must(c => !(c.ValidFromUtc.HasValue && c.ValidUntilUtc.HasValue)
                || c.ValidUntilUtc!.Value >= c.ValidFromUtc!.Value)
            .WithMessage("Validation.ValidUntilMustBeAfterValidFrom");
        RuleFor(x => x.CustomerGroupId).NotEmpty()
            .When(x => x.Scope == DiscountRuleScope.CustomerGroup)
            .WithMessage("Validation.CustomerGroupRequiredForScope");
        RuleFor(x => x.ProductCategoryId).NotEmpty()
            .When(x => x.Scope == DiscountRuleScope.ProductCategory)
            .WithMessage("Validation.ProductCategoryRequiredForScope");
        RuleFor(x => x.ProductId).NotEmpty()
            .When(x => x.Scope == DiscountRuleScope.Product)
            .WithMessage("Validation.ProductRequiredForScope");
    }
}

public class UpdateDiscountRuleCommandValidator : AbstractValidator<UpdateDiscountRuleCommand>
{
    public UpdateDiscountRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0m).WithMessage("Validation.DiscountValueMustBeNonNegative");
        RuleFor(x => x.Value).LessThanOrEqualTo(100m)
            .When(x => x.ValueType == DiscountValueType.Percent)
            .WithMessage("Validation.DiscountPercentRange");
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x).Must(c => !(c.ValidFromUtc.HasValue && c.ValidUntilUtc.HasValue)
                || c.ValidUntilUtc!.Value >= c.ValidFromUtc!.Value)
            .WithMessage("Validation.ValidUntilMustBeAfterValidFrom");
        RuleFor(x => x.CustomerGroupId).NotEmpty()
            .When(x => x.Scope == DiscountRuleScope.CustomerGroup)
            .WithMessage("Validation.CustomerGroupRequiredForScope");
        RuleFor(x => x.ProductCategoryId).NotEmpty()
            .When(x => x.Scope == DiscountRuleScope.ProductCategory)
            .WithMessage("Validation.ProductCategoryRequiredForScope");
        RuleFor(x => x.ProductId).NotEmpty()
            .When(x => x.Scope == DiscountRuleScope.Product)
            .WithMessage("Validation.ProductRequiredForScope");
    }
}

public class DeleteDiscountRuleCommandValidator : AbstractValidator<DeleteDiscountRuleCommand>
{
    public DeleteDiscountRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
