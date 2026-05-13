using CoreAlign.Application.Customers.Commands;
using FluentValidation;

namespace CoreAlign.Application.Customers.Validators;

public class CreateCustomerAddressCommandValidator : AbstractValidator<CreateCustomerAddressCommand>
{
    public CreateCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");
        RuleFor(x => x.Line1)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(200).WithMessage("Validation.TooLong");
        RuleFor(x => x.Line2).MaximumLength(200).WithMessage("Validation.TooLong");
        RuleFor(x => x.City).MaximumLength(100).WithMessage("Validation.TooLong");
        RuleFor(x => x.State).MaximumLength(100).WithMessage("Validation.TooLong");
        RuleFor(x => x.PostalCode).MaximumLength(32).WithMessage("Validation.TooLong");
        RuleFor(x => x.Country).MaximumLength(100).WithMessage("Validation.TooLong");
    }
}

public class UpdateCustomerAddressCommandValidator : AbstractValidator<UpdateCustomerAddressCommand>
{
    public UpdateCustomerAddressCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.TooLong");
        RuleFor(x => x.Line1)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(200).WithMessage("Validation.TooLong");
        RuleFor(x => x.Line2).MaximumLength(200).WithMessage("Validation.TooLong");
        RuleFor(x => x.City).MaximumLength(100).WithMessage("Validation.TooLong");
        RuleFor(x => x.State).MaximumLength(100).WithMessage("Validation.TooLong");
        RuleFor(x => x.PostalCode).MaximumLength(32).WithMessage("Validation.TooLong");
        RuleFor(x => x.Country).MaximumLength(100).WithMessage("Validation.TooLong");
    }
}
