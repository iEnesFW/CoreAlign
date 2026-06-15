using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Providers.Commands;

public sealed class UpsertTenantProviderConfigHandler
    : IRequestHandler<UpsertTenantProviderConfigCommand, TenantProviderConfigDto>
{
    private readonly ITenantProviderConfigRepository _repository;
    private readonly IProviderCredentialProtector _protector;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantProviderConfigResolver _resolver;

    public UpsertTenantProviderConfigHandler(
        ITenantProviderConfigRepository repository,
        IProviderCredentialProtector protector,
        ITenantContext tenantContext,
        ITenantProviderConfigResolver resolver)
    {
        _repository = repository;
        _protector = protector;
        _tenantContext = tenantContext;
        _resolver = resolver;
    }

    public async Task<TenantProviderConfigDto> Handle(
        UpsertTenantProviderConfigCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();

        var encryptedCredentials = !string.IsNullOrWhiteSpace(request.PlaintextCredentialsJson)
            ? _protector.Protect(tenantId, request.Category, request.PlaintextCredentialsJson)
            : null;

        var existing = await _repository.GetByTenantAndCategoryAsync(
            tenantId, request.Category, request.ProviderName, cancellationToken);

        TenantProviderConfig config;
        if (existing is null)
        {
            config = new TenantProviderConfig(
                request.Category,
                request.ProviderName,
                request.DisplayName,
                request.IsDefault,
                request.IsEnabled,
                encryptedCredentials,
                request.EnabledCapabilities)
            {
                TenantId = tenantId,
            };

            await _repository.AddAsync(config, cancellationToken);
        }
        else
        {
            config = existing;
            config.UpdateDisplayName(request.DisplayName);
            config.SetEnabled(request.IsEnabled);
            config.MarkAsDefault(request.IsDefault);
            config.UpdateCapabilities(request.EnabledCapabilities);
            if (encryptedCredentials is not null)
            {
                config.UpdateCredentials(encryptedCredentials);
            }

            _repository.Update(config);
        }

        if (request.IsDefault)
        {
            var siblings = await _repository.ListByTenantAsync(tenantId, request.Category, cancellationToken);
            foreach (var sibling in siblings)
            {
                if (sibling.Id != config.Id && sibling.IsDefault)
                {
                    sibling.MarkAsDefault(false);
                    _repository.Update(sibling);
                }
            }
        }

        await _resolver.InvalidateCacheAsync(tenantId, request.Category);

        return Map(config);
    }

    internal static TenantProviderConfigDto Map(TenantProviderConfig config) => new(
        config.Id,
        config.Category.ToString(),
        config.ProviderName,
        config.DisplayName,
        config.IsDefault,
        config.IsEnabled,
        config.EnabledCapabilities,
        config.LastHealthCheckUtc,
        config.LastHealthStatus.ToString(),
        config.LastHealthMessage);
}
