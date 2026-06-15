using FluentValidation;

namespace CoreAlign.Application.Profile.Notifications;

public sealed class UpdateProfileNotificationPreferencesValidator
    : AbstractValidator<UpdateProfileNotificationPreferencesCommand>
{
    public UpdateProfileNotificationPreferencesValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(child =>
        {
            child.RuleFor(i => i.NotificationKind).NotEmpty().MaximumLength(64);
        });
    }
}
