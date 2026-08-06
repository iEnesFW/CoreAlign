using System.Text.Json;
using CoreAlign.Application.Notifications.Smtp;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Tests.Notifications;

public class TenantSmtpSettingsTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private sealed class Harness
    {
        public ITenantContext TenantContext { get; } = Substitute.For<ITenantContext>();
        public ITenantProviderConfigRepository Repository { get; } = Substitute.For<ITenantProviderConfigRepository>();
        public IProviderCredentialProtector Protector { get; } = Substitute.For<IProviderCredentialProtector>();
        public IMediator Mediator { get; } = Substitute.For<IMediator>();
        public UpsertTenantProviderConfigCommand? Sent { get; private set; }

        public Harness(SmtpCredentials? existing = null)
        {
            TenantContext.RequireTenantId().Returns(TenantId);

            if (existing is not null)
            {
                var config = new TenantProviderConfig(
                    ProviderCategory.Email, "smtp", "SMTP", isDefault: true, isEnabled: true,
                    encryptedCredentialsJson: "cipher");
                Repository
                    .GetByTenantAndCategoryAsync(TenantId, ProviderCategory.Email, "smtp", Arg.Any<CancellationToken>())
                    .Returns(config);
                Protector
                    .UnprotectAs<SmtpCredentials>(TenantId, ProviderCategory.Email, "cipher")
                    .Returns(existing);
            }

            Mediator
                .Send(Arg.Do<UpsertTenantProviderConfigCommand>(c => Sent = c), Arg.Any<CancellationToken>())
                .Returns(_ => new TenantProviderConfigDto(
                    Guid.NewGuid(),
                    nameof(ProviderCategory.Email),
                    "smtp",
                    "SMTP",
                    true,
                    true,
                    0,
                    null,
                    nameof(ProviderHealthStatus.Unknown),
                    null));
        }

        public UpsertTenantSmtpSettingsHandler Handler() =>
            new(TenantContext, Repository, Protector, Mediator);

        public SmtpCredentials Persisted() =>
            JsonSerializer.Deserialize<SmtpCredentials>(Sent!.PlaintextCredentialsJson!)!;
    }

    private static UpsertTenantSmtpSettingsCommand OAuthCommand(
        string? clientSecret = "secret",
        string? refreshToken = "refresh") => new(
            "smtp.gmail.com",
            587,
            true,
            "mailbox@example.com",
            null,
            "mailbox@example.com",
            "CoreAlign",
            true,
            SmtpAuthModes.OAuth2,
            SmtpOAuthProviders.Google,
            null,
            "client-id",
            clientSecret,
            refreshToken,
            null,
            null);

    [Fact]
    public async Task An_oauth_configuration_is_persisted_with_its_grant_material()
    {
        var harness = new Harness();

        await harness.Handler().Handle(OAuthCommand(), CancellationToken.None);

        var saved = harness.Persisted();
        saved.UsesOAuth.Should().BeTrue();
        saved.OAuthProvider.Should().Be(SmtpOAuthProviders.Google);
        saved.OAuthClientId.Should().Be("client-id");
        saved.OAuthRefreshToken.Should().Be("refresh");
    }

    [Fact]
    public async Task Blank_oauth_secrets_keep_the_stored_ones_instead_of_wiping_them()
    {
        var existing = new SmtpCredentials(
            "smtp.gmail.com", 587, true, "mailbox@example.com", "old-password", "mailbox@example.com", "CoreAlign",
            SmtpAuthModes.OAuth2, SmtpOAuthProviders.Google, null, "client-id", "stored-secret", "stored-refresh", null, null);
        var harness = new Harness(existing);

        await harness.Handler().Handle(OAuthCommand(clientSecret: null, refreshToken: "   "), CancellationToken.None);

        var saved = harness.Persisted();
        saved.OAuthClientSecret.Should().Be("stored-secret");
        saved.OAuthRefreshToken.Should().Be("stored-refresh");
        saved.Password.Should().Be("old-password");
    }

    [Fact]
    public async Task An_unusable_oauth_configuration_is_refused_before_it_is_stored()
    {
        var harness = new Harness();
        var command = OAuthCommand(clientSecret: null, refreshToken: null);

        var act = () => harness.Handler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<SmtpOAuthConfigurationException>();
        harness.Sent.Should().BeNull();
    }

    [Fact]
    public async Task Switching_back_to_password_auth_clears_the_oauth_mode()
    {
        var harness = new Harness();
        var command = new UpsertTenantSmtpSettingsCommand(
            "smtp.example.com", 587, true, "user", "pass", "from@example.com", "CoreAlign", true);

        await harness.Handler().Handle(command, CancellationToken.None);

        var saved = harness.Persisted();
        saved.UsesOAuth.Should().BeFalse();
        saved.AuthMode.Should().Be(SmtpAuthModes.Password);
    }

    [Fact]
    public async Task The_read_model_reports_secret_presence_without_leaking_the_secrets()
    {
        var existing = new SmtpCredentials(
            "smtp.gmail.com", 587, true, "mailbox@example.com", null, "mailbox@example.com", "CoreAlign",
            SmtpAuthModes.OAuth2, SmtpOAuthProviders.Google, "common", "client-id", "top-secret", "refresh-token", null, "scope");
        var harness = new Harness(existing);
        var handler = new GetTenantSmtpSettingsHandler(harness.TenantContext, harness.Repository, harness.Protector);

        var dto = await handler.Handle(new GetTenantSmtpSettingsQuery(), CancellationToken.None);

        dto.AuthMode.Should().Be(SmtpAuthModes.OAuth2);
        dto.OAuthClientId.Should().Be("client-id");
        dto.OAuthTenantId.Should().Be("common");
        dto.HasOAuthClientSecret.Should().BeTrue();
        dto.HasOAuthRefreshToken.Should().BeTrue();
        JsonSerializer.Serialize(dto).Should().NotContain("top-secret").And.NotContain("refresh-token");
    }
}

public class UpsertTenantSmtpSettingsValidatorTests
{
    private static readonly UpsertTenantSmtpSettingsCommandValidator Validator = new();

    private static UpsertTenantSmtpSettingsCommand OAuth(
        string? provider = SmtpOAuthProviders.Google,
        string? clientId = "client-id",
        string? username = "mailbox@example.com",
        string? fromAddress = "mailbox@example.com",
        string? endpoint = null) => new(
            "smtp.gmail.com", 587, true, username, null, fromAddress, "CoreAlign", true,
            SmtpAuthModes.OAuth2, provider, null, clientId, "secret", "refresh", endpoint, null);

    [Fact]
    public void A_complete_google_oauth_configuration_passes()
    {
        Validator.Validate(OAuth()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void An_unknown_auth_mode_is_refused()
    {
        var command = OAuth() with { AuthMode = "Kerberos" };

        Validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Oauth_without_a_client_id_is_refused()
    {
        Validator.Validate(OAuth(clientId: null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Oauth_without_a_mailbox_address_is_refused()
    {
        Validator.Validate(OAuth(username: null, fromAddress: null)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void A_custom_provider_without_a_token_endpoint_is_refused()
    {
        Validator.Validate(OAuth(provider: SmtpOAuthProviders.Custom)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Password_auth_is_unaffected_by_the_oauth_rules()
    {
        var command = new UpsertTenantSmtpSettingsCommand(
            "smtp.example.com", 587, true, "user", "pass", "from@example.com", "CoreAlign", true);

        Validator.Validate(command).IsValid.Should().BeTrue();
    }
}
