using System.Text.Json;
using CoreAlign.Application.Notifications.Providers;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Commands;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Notifications.Smtp;

internal static class SmtpProvider
{
    public const string Name = "smtp";
    public const ProviderCategory Category = ProviderCategory.Email;
}

internal static class SmtpSettingsMapper
{
    public static TenantSmtpSettingsDto Map(TenantProviderConfig? config, SmtpCredentials? credentials)
    {
        if (config is null)
        {
            return new TenantSmtpSettingsDto(false, false, string.Empty, 587, true, null, null, null, false, null, null);
        }

        return new TenantSmtpSettingsDto(
            true,
            config.IsEnabled,
            credentials?.Host ?? string.Empty,
            credentials?.Port ?? 587,
            credentials?.UseSsl ?? true,
            credentials?.Username,
            credentials?.FromAddress,
            credentials?.FromName,
            !string.IsNullOrEmpty(credentials?.Password),
            config.LastHealthStatus.ToString(),
            config.LastHealthCheckUtc,
            credentials?.UsesOAuth == true ? SmtpAuthModes.OAuth2 : SmtpAuthModes.Password,
            credentials?.OAuthProvider,
            credentials?.OAuthTenantId,
            credentials?.OAuthClientId,
            credentials?.OAuthTokenEndpoint,
            credentials?.OAuthScope,
            !string.IsNullOrEmpty(credentials?.OAuthClientSecret),
            !string.IsNullOrEmpty(credentials?.OAuthRefreshToken));
    }
}

public sealed class GetTenantSmtpSettingsHandler : IRequestHandler<GetTenantSmtpSettingsQuery, TenantSmtpSettingsDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantProviderConfigRepository _repository;
    private readonly IProviderCredentialProtector _protector;

    public GetTenantSmtpSettingsHandler(
        ITenantContext tenantContext,
        ITenantProviderConfigRepository repository,
        IProviderCredentialProtector protector)
    {
        _tenantContext = tenantContext;
        _repository = repository;
        _protector = protector;
    }

    public async Task<TenantSmtpSettingsDto> Handle(GetTenantSmtpSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var config = await _repository.GetByTenantAndCategoryAsync(tenantId, SmtpProvider.Category, SmtpProvider.Name, cancellationToken);
        var credentials = config?.EncryptedCredentialsJson is null
            ? null
            : _protector.UnprotectAs<SmtpCredentials>(tenantId, SmtpProvider.Category, config.EncryptedCredentialsJson);
        return SmtpSettingsMapper.Map(config, credentials);
    }
}

public sealed class UpsertTenantSmtpSettingsHandler : IRequestHandler<UpsertTenantSmtpSettingsCommand, TenantSmtpSettingsDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantProviderConfigRepository _repository;
    private readonly IProviderCredentialProtector _protector;
    private readonly IMediator _mediator;

    public UpsertTenantSmtpSettingsHandler(
        ITenantContext tenantContext,
        ITenantProviderConfigRepository repository,
        IProviderCredentialProtector protector,
        IMediator mediator)
    {
        _tenantContext = tenantContext;
        _repository = repository;
        _protector = protector;
        _mediator = mediator;
    }

    public async Task<TenantSmtpSettingsDto> Handle(UpsertTenantSmtpSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();

        var password = NullIfEmpty(request.Password);
        var clientSecret = NullIfEmpty(request.OAuthClientSecret);
        var refreshToken = NullIfEmpty(request.OAuthRefreshToken);
        if (password is null || clientSecret is null || refreshToken is null)
        {
            var existing = await _repository.GetByTenantAndCategoryAsync(tenantId, SmtpProvider.Category, SmtpProvider.Name, cancellationToken);
            if (existing?.EncryptedCredentialsJson is not null)
            {
                var prior = _protector.UnprotectAs<SmtpCredentials>(tenantId, SmtpProvider.Category, existing.EncryptedCredentialsJson);
                password ??= prior?.Password;
                clientSecret ??= prior?.OAuthClientSecret;
                refreshToken ??= prior?.OAuthRefreshToken;
            }
        }

        var authMode = string.Equals(request.AuthMode, SmtpAuthModes.OAuth2, StringComparison.OrdinalIgnoreCase)
            ? SmtpAuthModes.OAuth2
            : SmtpAuthModes.Password;

        var credentials = new SmtpCredentials(
            request.Host.Trim(),
            request.Port,
            request.UseSsl,
            NullIfEmpty(request.Username),
            password,
            NullIfEmpty(request.FromAddress),
            NullIfEmpty(request.FromName),
            authMode,
            NullIfEmpty(request.OAuthProvider),
            NullIfEmpty(request.OAuthTenantId),
            NullIfEmpty(request.OAuthClientId),
            clientSecret,
            refreshToken,
            NullIfEmpty(request.OAuthTokenEndpoint),
            NullIfEmpty(request.OAuthScope));

        if (authMode == SmtpAuthModes.OAuth2)
        {
            SmtpOAuthResolver.Resolve(credentials);
        }

        var json = JsonSerializer.Serialize(credentials);

        await _mediator.Send(
            new UpsertTenantProviderConfigCommand(
                SmtpProvider.Category,
                SmtpProvider.Name,
                "SMTP",
                IsDefault: true,
                IsEnabled: request.IsEnabled,
                PlaintextCredentialsJson: json,
                EnabledCapabilities: 0),
            cancellationToken);

        var config = await _repository.GetByTenantAndCategoryAsync(tenantId, SmtpProvider.Category, SmtpProvider.Name, cancellationToken);
        return SmtpSettingsMapper.Map(config, credentials);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SendTestEmailHandler : IRequestHandler<SendTestEmailCommand, SendTestEmailResult>
{
    private const string Subject = "CoreAlign SMTP test";
    private const string BodyHtml = "<p>This is a test message confirming your CoreAlign SMTP settings are working.</p>";
    private const string BodyText = "This is a test message confirming your CoreAlign SMTP settings are working.";

    private readonly IProviderRegistry<IEmailProvider> _emailRegistry;

    public SendTestEmailHandler(IProviderRegistry<IEmailProvider> emailRegistry) => _emailRegistry = emailRegistry;

    public async Task<SendTestEmailResult> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        var provider = _emailRegistry.Find(SmtpProvider.Name);
        if (provider is null)
        {
            return new SendTestEmailResult(false, "SMTP provider is not available.");
        }

        var message = new EmailMessage(string.Empty, string.Empty, request.ToAddress.Trim(), Subject, BodyHtml, BodyText, null);
        var result = await provider.SendAsync(message, cancellationToken);
        return new SendTestEmailResult(result.Success, result.Success ? "Test email accepted for delivery." : result.FailureReason);
    }
}

public sealed class CheckSmtpHealthHandler : IRequestHandler<CheckSmtpHealthQuery, SmtpHealthResult>
{
    private readonly ITenantContext _tenantContext;
    private readonly IProviderRegistry<IEmailProvider> _emailRegistry;

    public CheckSmtpHealthHandler(ITenantContext tenantContext, IProviderRegistry<IEmailProvider> emailRegistry)
    {
        _tenantContext = tenantContext;
        _emailRegistry = emailRegistry;
    }

    public async Task<SmtpHealthResult> Handle(CheckSmtpHealthQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var provider = _emailRegistry.Find(SmtpProvider.Name);
        if (provider is null)
        {
            return new SmtpHealthResult(false, "SMTP provider is not available.", DateTime.UtcNow);
        }

        var health = await provider.CheckHealthAsync(tenantId, cancellationToken);
        return new SmtpHealthResult(health.IsHealthy, health.Message, health.CheckedAtUtc);
    }
}
