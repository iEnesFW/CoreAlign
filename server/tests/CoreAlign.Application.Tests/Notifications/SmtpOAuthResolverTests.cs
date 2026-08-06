using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Notifications;

public class SmtpOAuthResolverTests
{
    private static SmtpCredentials OAuth(
        string provider,
        string? clientId = "client-id",
        string? clientSecret = null,
        string? refreshToken = "refresh-token",
        string? tenantId = null,
        string? endpoint = null,
        string? scope = null) => new(
            "smtp.example.com",
            587,
            true,
            "mailbox@example.com",
            null,
            "mailbox@example.com",
            "CoreAlign",
            SmtpAuthModes.OAuth2,
            provider,
            tenantId,
            clientId,
            clientSecret,
            refreshToken,
            endpoint,
            scope);

    [Fact]
    public void Google_resolves_the_published_endpoint_and_mail_scope()
    {
        var settings = SmtpOAuthResolver.Resolve(OAuth(SmtpOAuthProviders.Google));

        settings.TokenEndpoint.Should().Be("https://oauth2.googleapis.com/token");
        settings.GrantType.Should().Be(SmtpOAuthGrantTypes.RefreshToken);
        settings.Scope.Should().Be("https://mail.google.com/");
        settings.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public void Microsoft_builds_the_directory_scoped_endpoint()
    {
        var settings = SmtpOAuthResolver.Resolve(
            OAuth(SmtpOAuthProviders.Microsoft, tenantId: "contoso.onmicrosoft.com"));

        settings.TokenEndpoint.Should()
            .Be("https://login.microsoftonline.com/contoso.onmicrosoft.com/oauth2/v2.0/token");
        settings.Scope.Should().Contain("SMTP.Send");
    }

    [Fact]
    public void Microsoft_without_a_directory_falls_back_to_common()
    {
        var settings = SmtpOAuthResolver.Resolve(OAuth(SmtpOAuthProviders.Microsoft));

        settings.TokenEndpoint.Should().Be("https://login.microsoftonline.com/common/oauth2/v2.0/token");
    }

    [Fact]
    public void A_client_secret_without_a_refresh_token_uses_the_client_credentials_grant()
    {
        var settings = SmtpOAuthResolver.Resolve(
            OAuth(SmtpOAuthProviders.Microsoft, clientSecret: "secret", refreshToken: null));

        settings.GrantType.Should().Be(SmtpOAuthGrantTypes.ClientCredentials);
        settings.Scope.Should().Be("https://outlook.office365.com/.default");
    }

    [Fact]
    public void Neither_a_refresh_token_nor_a_client_secret_is_refused()
    {
        var act = () => SmtpOAuthResolver.Resolve(
            OAuth(SmtpOAuthProviders.Google, clientSecret: null, refreshToken: null));

        act.Should().Throw<SmtpOAuthConfigurationException>();
    }

    [Fact]
    public void A_missing_client_id_is_refused()
    {
        var act = () => SmtpOAuthResolver.Resolve(OAuth(SmtpOAuthProviders.Google, clientId: " "));

        act.Should().Throw<SmtpOAuthConfigurationException>();
    }

    [Fact]
    public void A_custom_provider_requires_an_explicit_endpoint()
    {
        var act = () => SmtpOAuthResolver.Resolve(OAuth(SmtpOAuthProviders.Custom));

        act.Should().Throw<SmtpOAuthConfigurationException>();
    }

    [Fact]
    public void A_custom_provider_accepts_a_public_https_endpoint()
    {
        var settings = SmtpOAuthResolver.Resolve(
            OAuth(SmtpOAuthProviders.Custom, endpoint: "https://idp.example.com/oauth/token", scope: "mail.send"));

        settings.TokenEndpoint.Should().Be("https://idp.example.com/oauth/token");
        settings.Scope.Should().Be("mail.send");
    }

    [Theory]
    [InlineData("http://idp.example.com/token")]
    [InlineData("https://localhost/token")]
    [InlineData("https://127.0.0.1/token")]
    [InlineData("https://10.0.0.5/token")]
    [InlineData("https://192.168.1.10/token")]
    [InlineData("https://172.16.4.4/token")]
    [InlineData("https://169.254.169.254/token")]
    [InlineData("https://vault.internal/token")]
    [InlineData("https://user:pass@idp.example.com/token")]
    [InlineData("not-a-url")]
    public void Internal_or_unsafe_token_endpoints_are_refused(string endpoint)
    {
        var act = () => SmtpOAuthResolver.ValidateEndpoint(endpoint);

        act.Should().Throw<SmtpOAuthConfigurationException>();
    }

    [Fact]
    public void A_password_credential_is_not_treated_as_oauth()
    {
        var credentials = new SmtpCredentials(
            "smtp.example.com", 587, true, "user", "pass", "from@example.com", "CoreAlign");

        credentials.UsesOAuth.Should().BeFalse();
    }
}
