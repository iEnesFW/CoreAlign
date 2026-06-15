using System.Text.Json;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Admin;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicies.ProviderConfig)]
[Route("api/v{version:apiVersion}/admin/providers")]
public class ProviderListController : ControllerBase
{
    private readonly IProviderRegistry<IEFaturaProvider> _eFaturaRegistry;
    private readonly IProviderRegistry<IPaymentProvider> _paymentRegistry;
    private readonly ITenantProviderConfigRepository _configRepository;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ITenantContext _tenantContext;

    public ProviderListController(
        IProviderRegistry<IEFaturaProvider> eFaturaRegistry,
        IProviderRegistry<IPaymentProvider> paymentRegistry,
        ITenantProviderConfigRepository configRepository,
        IProviderCredentialProtector credentialProtector,
        ITenantContext tenantContext)
    {
        _eFaturaRegistry = eFaturaRegistry;
        _paymentRegistry = paymentRegistry;
        _configRepository = configRepository;
        _credentialProtector = credentialProtector;
        _tenantContext = tenantContext;
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> ListCatalog(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenantConfigs = await _configRepository.ListByTenantAsync(tenantId, category: null, cancellationToken);
        var configsByKey = tenantConfigs.ToDictionary(
            c => CatalogKey(c.Category, c.ProviderName),
            StringComparer.OrdinalIgnoreCase);

        var results = new List<ProviderInfoDto>();

        foreach (var provider in _eFaturaRegistry.All)
        {
            results.Add(BuildDto(provider, ProviderCategory.EFatura, tenantId, configsByKey));
        }

        foreach (var provider in _paymentRegistry.All)
        {
            results.Add(BuildDto(provider, ProviderCategory.Payment, tenantId, configsByKey));
        }

        return Ok(ApiResponse<IReadOnlyList<ProviderInfoDto>>.Success(results));
    }

    private ProviderInfoDto BuildDto(
        IExternalProvider provider,
        ProviderCategory category,
        Guid tenantId,
        IReadOnlyDictionary<string, TenantProviderConfig> configsByKey)
    {
        configsByKey.TryGetValue(CatalogKey(category, provider.Name), out var config);

        var isConfigured = config is not null && !string.IsNullOrWhiteSpace(config.EncryptedCredentialsJson);
        var isSandbox = isConfigured && IsSandboxConfigured(tenantId, category, config!.EncryptedCredentialsJson);

        var capabilityList = BuildCapabilityList(provider.Capabilities);

        return new ProviderInfoDto(
            Name: provider.Name,
            DisplayName: provider.DisplayName,
            Category: category.ToString(),
            IsConfigured: isConfigured,
            IsEnabled: config?.IsEnabled ?? false,
            IsDefault: config?.IsDefault ?? false,
            IsSandbox: isSandbox,
            LastHealthStatus: ResolveHealthStatusForResponse(config, isConfigured),
            LastHealthMessage: config?.LastHealthMessage,
            LastHealthCheckedUtc: config?.LastHealthCheckUtc,
            LastUsedAtUtc: config?.UpdatedAtUtc,
            Capabilities: capabilityList);
    }

    private static string ResolveHealthStatusForResponse(TenantProviderConfig? config, bool isConfigured)
    {
        if (!isConfigured)
        {
            return ProviderHealthStatus.NotConfigured.ToString();
        }
        return (config?.LastHealthStatus ?? ProviderHealthStatus.Unknown).ToString();
    }

    private static IReadOnlyList<string> BuildCapabilityList(ProviderCapabilities capabilities)
    {
        var output = new List<string>();
        foreach (ProviderCapability flag in Enum.GetValues<ProviderCapability>())
        {
            if (flag == ProviderCapability.None) continue;
            if (capabilities.Has(flag))
            {
                output.Add(flag.ToString());
            }
        }
        return output;
    }

    private bool IsSandboxConfigured(Guid tenantId, ProviderCategory category, string? encryptedCredentialsJson)
    {
        if (string.IsNullOrWhiteSpace(encryptedCredentialsJson))
        {
            return false;
        }

        if (!_credentialProtector.TryUnprotect(tenantId, category, encryptedCredentialsJson, out var plaintext)
            || string.IsNullOrWhiteSpace(plaintext))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(plaintext);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;

            foreach (var key in new[] { "isSandbox", "IsSandbox", "sandbox" })
            {
                if (doc.RootElement.TryGetProperty(key, out var element))
                {
                    if (element.ValueKind == JsonValueKind.True) return true;
                    if (element.ValueKind == JsonValueKind.String
                        && bool.TryParse(element.GetString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static string CatalogKey(ProviderCategory category, string providerName) =>
        $"{(int)category}:{providerName.ToLowerInvariant()}";
}
