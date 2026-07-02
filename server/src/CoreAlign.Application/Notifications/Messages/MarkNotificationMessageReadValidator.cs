using FluentValidation;

namespace CoreAlign.Application.Notifications.Messages;

public sealed class MarkNotificationMessageReadValidator
    : AbstractValidator<MarkNotificationMessageReadCommand>
{
    public MarkNotificationMessageReadValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
