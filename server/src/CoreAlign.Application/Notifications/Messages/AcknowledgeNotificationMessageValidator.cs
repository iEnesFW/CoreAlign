using FluentValidation;

namespace CoreAlign.Application.Notifications.Messages;

public sealed class AcknowledgeNotificationMessageValidator
    : AbstractValidator<AcknowledgeNotificationMessageCommand>
{
    public AcknowledgeNotificationMessageValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Note)
            .MaximumLength(2000)
            .WithMessage("Validation.NoteTooLong")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
