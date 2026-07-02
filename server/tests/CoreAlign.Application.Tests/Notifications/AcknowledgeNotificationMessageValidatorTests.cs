using CoreAlign.Application.Notifications.Messages;

namespace CoreAlign.Application.Tests.Notifications;

public class AcknowledgeNotificationMessageValidatorTests
{
    private readonly AcknowledgeNotificationMessageValidator _validator = new();

    [Fact]
    public void Null_or_short_note_passes()
    {
        _validator.Validate(new AcknowledgeNotificationMessageCommand(Guid.NewGuid(), null, Guid.NewGuid()))
            .IsValid.Should().BeTrue();
        _validator.Validate(new AcknowledgeNotificationMessageCommand(Guid.NewGuid(), "ok", Guid.NewGuid()))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Note_over_2000_chars_fails()
    {
        var result = _validator.Validate(
            new AcknowledgeNotificationMessageCommand(Guid.NewGuid(), new string('x', 2001), Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note");
    }

    [Fact]
    public void Empty_message_id_fails()
    {
        _validator.Validate(new AcknowledgeNotificationMessageCommand(Guid.Empty, "note", Guid.NewGuid()))
            .IsValid.Should().BeFalse();
    }
}
