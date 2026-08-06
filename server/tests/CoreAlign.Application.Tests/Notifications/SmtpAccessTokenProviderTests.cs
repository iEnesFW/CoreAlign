using System.Net;
using System.Text;
using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Infrastructure.Notifications.Email;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Notifications;

public class SmtpAccessTokenProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        public int Calls { get; private set; }
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (request.Content is not null)
            {
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }
            return _responder(request);
        }
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private static SmtpAccessTokenProvider Build(StubHandler handler, IMemoryCache? cache = null) =>
        new(new StubFactory(handler),
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SmtpAccessTokenProvider>.Instance);

    private static SmtpOAuthSettings Settings(string clientId = "client-id") => new(
        "https://oauth2.googleapis.com/token",
        SmtpOAuthGrantTypes.RefreshToken,
        clientId,
        "client-secret",
        "refresh-token",
        "https://mail.google.com/");

    [Fact]
    public async Task An_access_token_is_returned_and_the_grant_is_posted_as_a_form()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{"access_token":"ya29.token","expires_in":3599}"""));
        var sut = Build(handler);

        var token = await sut.GetAccessTokenAsync(Settings(), CancellationToken.None);

        token.Should().Be("ya29.token");
        handler.Calls.Should().Be(1);
        handler.Bodies[0].Should().Contain("grant_type=refresh_token");
        handler.Bodies[0].Should().Contain("client_id=client-id");
        handler.Bodies[0].Should().Contain("refresh_token=refresh-token");
    }

    [Fact]
    public async Task A_live_token_is_served_from_cache_instead_of_a_second_round_trip()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{"access_token":"ya29.token","expires_in":3599}"""));
        var sut = Build(handler);

        await sut.GetAccessTokenAsync(Settings(), CancellationToken.None);
        var second = await sut.GetAccessTokenAsync(Settings(), CancellationToken.None);

        second.Should().Be("ya29.token");
        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Different_credentials_never_share_a_cached_token()
    {
        var responses = new Queue<string>(new[]
        {
            """{"access_token":"first","expires_in":3599}""",
            """{"access_token":"second","expires_in":3599}""",
        });
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, responses.Dequeue()));
        var sut = Build(handler);

        var first = await sut.GetAccessTokenAsync(Settings("tenant-a"), CancellationToken.None);
        var second = await sut.GetAccessTokenAsync(Settings("tenant-b"), CancellationToken.None);

        first.Should().Be("first");
        second.Should().Be("second");
        handler.Calls.Should().Be(2);
    }

    [Theory]
    [InlineData(3599, 3539)]
    [InlineData(120, 60)]
    [InlineData(70, 30)]
    [InlineData(5, 30)]
    [InlineData(null, 240)]
    [InlineData(0, 240)]
    public void The_cache_window_stops_a_minute_before_the_token_expires(int? expiresIn, int expectedSeconds)
    {
        SmtpAccessTokenProvider.ComputeCacheTtl(expiresIn)
            .Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public async Task A_rejected_grant_surfaces_the_providers_reason()
    {
        var handler = new StubHandler(_ => Json(
            HttpStatusCode.BadRequest,
            """{"error":"invalid_grant","error_description":"Token has been expired or revoked."}"""));
        var sut = Build(handler);

        var act = () => sut.GetAccessTokenAsync(Settings(), CancellationToken.None);

        (await act.Should().ThrowAsync<SmtpOAuthTokenException>())
            .WithMessage("*Token has been expired or revoked.*");
    }

    [Fact]
    public async Task A_success_response_without_a_token_is_refused()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{"token_type":"Bearer"}"""));
        var sut = Build(handler);

        var act = () => sut.GetAccessTokenAsync(Settings(), CancellationToken.None);

        await act.Should().ThrowAsync<SmtpOAuthTokenException>();
    }

    [Fact]
    public async Task An_internal_token_endpoint_is_refused_before_any_request_is_made()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{"access_token":"nope","expires_in":3599}"""));
        var sut = Build(handler);
        var settings = Settings() with { TokenEndpoint = "https://169.254.169.254/token" };

        var act = () => sut.GetAccessTokenAsync(settings, CancellationToken.None);

        await act.Should().ThrowAsync<SmtpOAuthConfigurationException>();
        handler.Calls.Should().Be(0);
    }
}
