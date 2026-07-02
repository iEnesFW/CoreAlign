using CoreAlign.Application.Notifications.Messages;

namespace CoreAlign.Application.Tests.Notifications;

public class MarkNotificationMessageReadValidatorTests
{
    private readonly MarkNotificationMessageReadValidator _validator = new();

    [Fact]
    public void Valid_message_id_passes()
    {
        _validator.Validate(new MarkNotificationMessageReadCommand(Guid.NewGuid(), Guid.NewGuid()))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_message_id_fails()
    {
        _validator.Validate(new MarkNotificationMessageReadCommand(Guid.Empty, Guid.NewGuid()))
            .IsValid.Should().BeFalse();
    }
}
