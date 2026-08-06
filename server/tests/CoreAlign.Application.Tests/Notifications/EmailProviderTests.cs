using System.Net;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Application.Providers;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Notifications.Email;
using CoreAlign.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.Notifications;

public class EmailProviderTests
{
    private static EmailMessage SampleMessage(string to = "user@example.com") => new(
        From: "noreply@corealign.local",
        FromName: "CoreAlign",
        To: to,
        Subject: "Test Subject",
        BodyHtml: "<p>Hello</p>",
        BodyText: "Hello",
        ReplyTo: null);

    private static TenantAwareSmtpEmailProvider BuildSmtp(SmtpEmailOptions options)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CurrentTenantId.Returns((Guid?)null);
        var resolver = Substitute.For<ITenantProviderConfigResolver>();
        var protector = Substitute.For<IProviderCredentialProtector>();
        var tokenProvider = Substitute.For<ISmtpAccessTokenProvider>();
        return new TenantAwareSmtpEmailProvider(
            tenantContext,
            resolver,
            protector,
            tokenProvider,
            Options.Create(options),
            NullLogger<TenantAwareSmtpEmailProvider>.Instance);
    }

    [Fact]
    public async Task SmtpEmailProvider_returns_failure_when_host_not_configured()
    {
        var sut = BuildSmtp(new SmtpEmailOptions { Host = string.Empty });

        var result = await sut.SendAsync(SampleMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("SMTP host not configured");
    }

    [Fact]
    public async Task SmtpEmailProvider_returns_failure_when_host_unreachable()
    {
        var sut = BuildSmtp(new SmtpEmailOptions
        {
            Host = "127.0.0.1",
            Port = 1,
            UseSsl = false,
            Username = string.Empty,
            Password = string.Empty
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await sut.SendAsync(SampleMessage(), cts.Token);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SmtpEmailProvider_exposes_name_and_capabilities()
    {
        var sut = BuildSmtp(new SmtpEmailOptions { Host = "smtp.example.com" });

        sut.Name.Should().Be("smtp");
        sut.DisplayName.Should().Contain("SMTP");
        sut.Capabilities.Metadata["transport"].Should().Be("smtp");
    }

    [Fact]
    public async Task SendGridEmailProvider_returns_failure_when_api_key_missing()
    {
        var options = Options.Create(new SendGridOptions { ApiKey = string.Empty });
        var factory = new SingleClientHttpFactory(new StubHandler(HttpStatusCode.OK, "{}"));
        var sut = new SendGridEmailProvider(factory, options, NullLogger<SendGridEmailProvider>.Instance);

        var result = await sut.SendAsync(SampleMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("SendGrid ApiKey");
    }

    [Fact]
    public async Task SendGridEmailProvider_sends_payload_and_returns_provider_message_id_on_success()
    {
        var options = Options.Create(new SendGridOptions
        {
            ApiKey = "SG.test-key",
            ApiBaseUrl = "https://api.sendgrid.com/v3"
        });
        var handler = new StubHandler(HttpStatusCode.Accepted, string.Empty, "msg-id-123");
        var factory = new SingleClientHttpFactory(handler);
        var sut = new SendGridEmailProvider(factory, options, NullLogger<SendGridEmailProvider>.Instance);

        var result = await sut.SendAsync(SampleMessage("dest@example.com"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("msg-id-123");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("mail/send");
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("SG.test-key");
        handler.CapturedBody.Should().Contain("dest@example.com");
        handler.CapturedBody.Should().Contain("Test Subject");
    }

    [Fact]
    public async Task SendGridEmailProvider_returns_failure_with_status_code_when_api_returns_error()
    {
        var options = Options.Create(new SendGridOptions
        {
            ApiKey = "SG.test-key",
            ApiBaseUrl = "https://api.sendgrid.com/v3"
        });
        var factory = new SingleClientHttpFactory(new StubHandler(HttpStatusCode.Unauthorized, "{\"errors\":[{\"message\":\"bad api key\"}]}"));
        var sut = new SendGridEmailProvider(factory, options, NullLogger<SendGridEmailProvider>.Instance);

        var result = await sut.SendAsync(SampleMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("401");
        result.FailureReason.Should().Contain("bad api key");
    }

    [Fact]
    public void SendGridEmailProvider_exposes_name_and_capabilities()
    {
        var options = Options.Create(new SendGridOptions { ApiKey = "SG.test" });
        var factory = new SingleClientHttpFactory(new StubHandler(HttpStatusCode.OK, "{}"));
        var sut = new SendGridEmailProvider(factory, options, NullLogger<SendGridEmailProvider>.Instance);

        sut.Name.Should().Be("sendgrid");
        sut.DisplayName.Should().Contain("SendGrid");
        sut.Capabilities.Metadata["transport"].Should().Be("https");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;
        private readonly string? _messageId;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string CapturedBody { get; private set; } = string.Empty;

        public StubHandler(HttpStatusCode statusCode, string body, string? messageId = null)
        {
            _statusCode = statusCode;
            _body = body;
            _messageId = messageId;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                CapturedBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body)
            };
            if (!string.IsNullOrEmpty(_messageId))
            {
                response.Headers.Add("X-Message-Id", _messageId);
            }
            return response;
        }
    }

    private sealed class SingleClientHttpFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public SingleClientHttpFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
