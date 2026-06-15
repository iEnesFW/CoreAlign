using CoreAlign.Application.Tags.Commands;
using FluentValidation;

namespace CoreAlign.Application.Tags.Validators;

public class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.NameTooLong");

        RuleFor(x => x.ColorHex)
            .MaximumLength(9).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.ColorHex));
    }
}

public class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(64).WithMessage("Validation.NameTooLong");

        RuleFor(x => x.ColorHex)
            .MaximumLength(9).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.ColorHex));
    }
}
