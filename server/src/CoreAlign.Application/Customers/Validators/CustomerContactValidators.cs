using CoreAlign.Application.Customers.Commands;
using FluentValidation;

namespace CoreAlign.Application.Customers.Validators;

public class CreateCustomerContactCommandValidator : AbstractValidator<CreateCustomerContactCommand>
{
    public CreateCustomerContactCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.NameTooShort")
            .MaximumLength(150).WithMessage("Validation.NameTooLong");
        RuleFor(x => x.Role).MaximumLength(100).WithMessage("Validation.TooLong");
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Validation.InvalidEmail")
            .MaximumLength(200).WithMessage("Validation.EmailTooLong")
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).WithMessage("Validation.TooLong");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Validation.TooLong");
    }
}

public class UpdateCustomerContactCommandValidator : AbstractValidator<UpdateCustomerContactCommand>
{
    public UpdateCustomerContactCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.NameTooShort")
            .MaximumLength(150).WithMessage("Validation.NameTooLong");
        RuleFor(x => x.Role).MaximumLength(100).WithMessage("Validation.TooLong");
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Validation.InvalidEmail")
            .MaximumLength(200).WithMessage("Validation.EmailTooLong")
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).WithMessage("Validation.TooLong");
        RuleFor(x => x.Notes).MaximumLength(500).WithMessage("Validation.TooLong");
    }
}
