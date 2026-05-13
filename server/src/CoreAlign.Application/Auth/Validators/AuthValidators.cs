using FluentValidation;
using CoreAlign.Application.Auth.Commands;

namespace CoreAlign.Application.Auth.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Validation.Required")
            .EmailAddress().WithMessage("Validation.InvalidEmail");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Validation.Required");
    }
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(2).WithMessage("Validation.OrganizationNameTooShort")
            .MaximumLength(150).WithMessage("Validation.OrganizationNameTooLong");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(3).WithMessage("Validation.UsernameTooShort")
            .MaximumLength(64).WithMessage("Validation.UsernameTooLong")
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Validation.UsernameInvalidChars");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Validation.Required")
            .EmailAddress().WithMessage("Validation.InvalidEmail")
            .MaximumLength(256).WithMessage("Validation.EmailTooLong");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(8).WithMessage("Validation.PasswordTooShort")
            .Matches("[A-Z]").WithMessage("Validation.PasswordNeedsUppercase")
            .Matches("[a-z]").WithMessage("Validation.PasswordNeedsLowercase")
            .Matches("[0-9]").WithMessage("Validation.PasswordNeedsDigit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Validation.PasswordNeedsSpecial");
    }
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Validation.Required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(8).WithMessage("Validation.PasswordTooShort")
            .Matches("[A-Z]").WithMessage("Validation.PasswordNeedsUppercase")
            .Matches("[a-z]").WithMessage("Validation.PasswordNeedsLowercase")
            .Matches("[0-9]").WithMessage("Validation.PasswordNeedsDigit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Validation.PasswordNeedsSpecial");
    }
}

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Validation.Required")
            .EmailAddress().WithMessage("Validation.InvalidEmail");
    }
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Validation.Required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Validation.Required")
            .MinimumLength(8).WithMessage("Validation.PasswordTooShort")
            .Matches("[A-Z]").WithMessage("Validation.PasswordNeedsUppercase")
            .Matches("[a-z]").WithMessage("Validation.PasswordNeedsLowercase")
            .Matches("[0-9]").WithMessage("Validation.PasswordNeedsDigit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Validation.PasswordNeedsSpecial")
            .NotEqual(x => x.CurrentPassword).WithMessage("Validation.PasswordMustDiffer");
    }
}

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.FirstName)
            .MaximumLength(64).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(64).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("Validation.TooLong")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
    }
}
