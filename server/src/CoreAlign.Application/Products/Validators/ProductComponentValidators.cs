using CoreAlign.Application.Products.Commands;
using FluentValidation;

namespace CoreAlign.Application.Products.Validators;

public class AddProductComponentCommandValidator : AbstractValidator<AddProductComponentCommand>
{
    public AddProductComponentCommandValidator()
    {
        RuleFor(x => x.ParentProductId).NotEmpty();
        RuleFor(x => x.ComponentProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Validation.Positive");
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateProductComponentCommandValidator : AbstractValidator<UpdateProductComponentCommand>
{
    public UpdateProductComponentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ParentProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Validation.Positive");
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
