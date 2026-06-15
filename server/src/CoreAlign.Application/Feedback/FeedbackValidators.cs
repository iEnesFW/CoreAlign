using FluentValidation;

namespace CoreAlign.Application.Feedback;

public class CreateFeedbackCommandValidator : AbstractValidator<CreateFeedbackCommand>
{
    public CreateFeedbackCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.StepsToReproduce).MaximumLength(2000);
        RuleFor(x => x.Module).MaximumLength(100);
        RuleFor(x => x.PageUrl).MaximumLength(500);
    }
}
