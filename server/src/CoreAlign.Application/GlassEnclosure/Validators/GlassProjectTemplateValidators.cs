using CoreAlign.Application.GlassEnclosure.Commands;
using FluentValidation;

namespace CoreAlign.Application.GlassEnclosure.Validators;

public class SaveGlassProjectTemplateCommandValidator : AbstractValidator<SaveGlassProjectTemplateCommand>
{
    private const int MaxPayloadChars = 262144;

    public SaveGlassProjectTemplateCommandValidator()
    {
        RuleFor(x => x.Data.Name)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(200);
        RuleFor(x => x.Data.PayloadJson)
            .NotEmpty().WithMessage("Validation.Required")
            .MaximumLength(MaxPayloadChars);
    }
}
