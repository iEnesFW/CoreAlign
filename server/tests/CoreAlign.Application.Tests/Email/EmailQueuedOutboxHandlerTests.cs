using System.Text.Json;
using CoreAlign.Application.Common.Email;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Email;

public class EmailQueuedOutboxHandlerTests
{
    private readonly IEmailTemplateRepository _templates = Substitute.For<IEmailTemplateRepository>();
    private readonly IEmailRenderer _renderer = Substitute.For<IEmailRenderer>();
    private readonly IEmailSender _sender = Substitute.For<IEmailSender>();
    private readonly EmailQueuedOutboxHandler _sut;

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid TenantId = Guid.NewGuid();

    public EmailQueuedOutboxHandlerTests()
    {
        _sut = new EmailQueuedOutboxHandler(
            _templates,
            _renderer,
            _sender,
            NullLogger<EmailQueuedOutboxHandler>.Instance);
    }

    [Fact]
    public async Task Resolves_template_renders_and_sends_message()
    {
        var template = new EmailTemplate(
            code: "auth.password.reset",
            name: "Şifre sıfırlama",
            subject: "Şifre sıfırla, {{ email }}",
            body: "<p>Token: {{ resetToken }}</p>");

        _templates.GetByCodeAsync("auth.password.reset", "tr-TR", Arg.Any<CancellationToken>()).Returns(template);
        _renderer.Render(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(new RenderedEmail("Şifre sıfırla, ada@example.com", "<p>Token: rt-1</p>"));

        EmailMessage? captured = null;
        await _sender.SendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>());

        var payload = new EmailQueuedPayload(
            To: "ada@example.com",
            TemplateCode: "auth.password.reset",
            Locale: "tr-TR",
            TenantId: TenantId,
            ReplyTo: null,
            Context: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["email"] = "ada@example.com",
                ["resetToken"] = "rt-1",
            });
        var json = JsonSerializer.Serialize(payload, Json);

        var result = await _sut.HandleAsync(json, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().Be("Sent:auth.password.reset");
        captured.Should().NotBeNull();
        captured!.To.Should().Be("ada@example.com");
        captured.Subject.Should().Be("Şifre sıfırla, ada@example.com");
        captured.BodyHtml.Should().Contain("rt-1");
        captured.TenantId.Should().Be(TenantId);
        await _sender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_template_falls_back_to_inline_body_without_failing()
    {
        _templates.GetByCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EmailTemplate?)null);

        EmailMessage? captured = null;
        await _sender.SendAsync(Arg.Do<EmailMessage>(m => captured = m), Arg.Any<CancellationToken>());

        var payload = new EmailQueuedPayload(
            To: "grace@example.com",
            TemplateCode: "invoice.issued",
            Locale: "tr-TR",
            TenantId: TenantId,
            ReplyTo: null,
            Context: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["invoiceNumber"] = "INV-1",
                ["total"] = 250m,
            });
        var json = JsonSerializer.Serialize(payload, Json);

        var result = await _sut.HandleAsync(json, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        captured.Should().NotBeNull();
        captured!.Subject.Should().Be("[invoice.issued]");
        captured.BodyHtml.Should().Contain("INV-1");
        _renderer.DidNotReceive().Render(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>());
    }

    [Fact]
    public async Task Empty_recipient_short_circuits_without_sending()
    {
        var payload = new EmailQueuedPayload(
            To: string.Empty,
            TemplateCode: "auth.password.reset",
            Locale: "tr-TR",
            TenantId: TenantId,
            ReplyTo: null,
            Context: new Dictionary<string, object?>());
        var json = JsonSerializer.Serialize(payload, Json);

        var result = await _sut.HandleAsync(json, default);

        result.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        result.ResultOrError.Should().Be("NoRecipient");
        await _sender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replaying_same_payload_invokes_sender_each_time_relying_on_outbox_status()
    {
        var template = new EmailTemplate(
            code: "auth.password.reset",
            name: "Reset",
            subject: "subj",
            body: "<p>body</p>");
        _templates.GetByCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(template);
        _renderer.Render(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, object?>>())
            .Returns(new RenderedEmail("subj", "<p>body</p>"));

        var payload = new EmailQueuedPayload(
            To: "ada@example.com",
            TemplateCode: "auth.password.reset",
            Locale: "tr-TR",
            TenantId: TenantId,
            ReplyTo: null,
            Context: new Dictionary<string, object?>());
        var json = JsonSerializer.Serialize(payload, Json);

        var first = await _sut.HandleAsync(json, default);
        var second = await _sut.HandleAsync(json, default);

        first.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        second.Outcome.Should().Be(OutboxHandlerOutcome.Processed);
        await _sender.Received(2).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
