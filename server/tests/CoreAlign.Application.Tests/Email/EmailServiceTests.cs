using CoreAlign.Application.Auth.DTOs;
using CoreAlign.Application.Common.Email;
using CoreAlign.Infrastructure.Options;
using CoreAlign.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Email;

public sealed class EmailServiceTests
{
    private readonly IEmailSender _sender = Substitute.For<IEmailSender>();

    private EmailService Build(string provider = "Smtp", string? host = "smtp.test", string? appBaseUrl = "https://app.test")
    {
        var options = Options.Create(new EmailOptions
        {
            Provider = provider,
            AppBaseUrl = appBaseUrl,
            Smtp = new EmailSmtpOptions { Host = host, Port = 587, FromAddress = "no-reply@test" },
        });
        return new EmailService(_sender, options, NullLogger<EmailService>.Instance);
    }

    [Fact]
    public async Task Configured_smtp_dispatches_password_reset_with_link()
    {
        EmailMessage? captured = null;
        await _sender.SendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await Build().SendPasswordResetEmailAsync("alice@example.com", "raw-token-123");

        await _sender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        captured.Should().NotBeNull();
        captured!.To.Should().Be("alice@example.com");
        captured.BodyHtml.Should().Contain("https://app.test/reset-password?token=raw-token-123");
    }

    [Fact]
    public async Task LogOnly_provider_does_not_dispatch()
    {
        await Build(provider: "LogOnly").SendEmailVerificationAsync("bob@example.com", "tok");

        await _sender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_smtp_host_does_not_dispatch()
    {
        await Build(host: null).SendEmailVerificationAsync("bob@example.com", "tok");

        await _sender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sender_failure_is_swallowed_so_auth_flow_never_leaks()
    {
        _sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("smtp down")));

        var act = () => Build().SendPasswordResetEmailAsync("alice@example.com", "tok");

        await act.Should().NotThrowAsync();
        await _sender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancellation_is_not_swallowed()
    {
        _sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new OperationCanceledException()));

        var act = () => Build().SendPasswordResetEmailAsync("alice@example.com", "tok");

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Order_comment_body_is_html_encoded()
    {
        EmailMessage? captured = null;
        await _sender.SendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await Build().SendOrderCommentPostedAsync("carol@example.com", "Dealer", "<script>alert(1)</script>");

        captured.Should().NotBeNull();
        captured!.BodyHtml.Should().NotContain("<script>");
        captured.BodyHtml.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public async Task Security_alert_dispatches_to_payload_email()
    {
        EmailMessage? captured = null;
        await _sender.SendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>());

        await Build().SendSecurityAlertAsync(new SecurityAlertEmailPayload(
            Guid.NewGuid(), "RefreshTokenReuse", DateTime.UtcNow, "1.2.3.4", "agent", "dave@example.com"));

        await _sender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        captured!.To.Should().Be("dave@example.com");
    }

    [Fact]
    public async Task Security_alert_without_recipient_is_not_dispatched()
    {
        await Build().SendSecurityAlertAsync(new SecurityAlertEmailPayload(
            Guid.NewGuid(), "RefreshTokenReuse", DateTime.UtcNow, "1.2.3.4", "agent", null));

        await _sender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
