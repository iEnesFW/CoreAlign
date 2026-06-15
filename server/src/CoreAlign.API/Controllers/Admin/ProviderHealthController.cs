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
public class ProviderHealthController : ControllerBase
{
    private readonly IProviderRegistry<IEFaturaProvider> _eFaturaRegistry;
    private readonly IProviderRegistry<IPaymentProvider> _paymentRegistry;
    private readonly ITenantProviderConfigRepository _configRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProviderHealthController> _logger;

    public ProviderHealthController(
        IProviderRegistry<IEFaturaProvider> eFaturaRegistry,
        IProviderRegistry<IPaymentProvider> paymentRegistry,
        ITenantProviderConfigRepository configRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        ILogger<ProviderHealthController> logger)
    {
        _eFaturaRegistry = eFaturaRegistry;
        _paymentRegistry = paymentRegistry;
        _configRepository = configRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("{category}/{providerName}/health")]
    public async Task<IActionResult> CheckHealth(
        string category,
        string providerName,
        CancellationToken cancellationToken)
    {
        if (!ProviderCategoryParser.TryParse(category, out var parsedCategory))
        {
            return BadRequest(ApiResponse<object>.Failure($"Unknown provider category '{category}'.", 400));
        }

        if (string.IsNullOrWhiteSpace(providerName))
        {
            return BadRequest(ApiResponse<object>.Failure("Provider name is required.", 400));
        }

        var tenantId = _tenantContext.RequireTenantId();

        IExternalProvider? provider = ResolveProvider(parsedCategory, providerName);
        if (provider is null)
        {
            return NotFound(ApiResponse<object>.Failure($"Provider '{providerName}' not found in category '{parsedCategory}'.", 404));
        }

        var result = await SafeCheckHealthAsync(provider, tenantId, cancellationToken);

        var config = await _configRepository.GetByTenantAndCategoryAsync(tenantId, parsedCategory, providerName, cancellationToken);
        if (config is not null)
        {
            var status = result.IsHealthy ? ProviderHealthStatus.Healthy : ProviderHealthStatus.Unhealthy;
            config.RecordHealthCheck(status, result.Message, DateTime.UtcNow);
            _configRepository.Update(config);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dto = MapResult(result, parsedCategory);
        return Ok(ApiResponse<ProviderHealthSummaryDto>.Success(dto));
    }

    [HttpGet("health-all")]
    public async Task<IActionResult> CheckAll(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var configs = await _configRepository.ListByTenantAsync(tenantId, category: null, cancellationToken);

        var results = new List<ProviderHealthSummaryDto>(configs.Count);
        foreach (var config in configs.Where(c => c.IsEnabled))
        {
            IExternalProvider? provider = ResolveProvider(config.Category, config.ProviderName);
            if (provider is null)
            {
                continue;
            }

            var result = await SafeCheckHealthAsync(provider, tenantId, cancellationToken);
            var status = result.IsHealthy ? ProviderHealthStatus.Healthy : ProviderHealthStatus.Unhealthy;
            config.RecordHealthCheck(status, result.Message, DateTime.UtcNow);
            _configRepository.Update(config);

            results.Add(MapResult(result, config.Category));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProviderHealthSummaryDto>>.Success(results));
    }

    private IExternalProvider? ResolveProvider(ProviderCategory category, string providerName) =>
        category switch
        {
            ProviderCategory.EFatura => _eFaturaRegistry.Find(providerName),
            ProviderCategory.Payment => _paymentRegistry.Find(providerName),
            _ => null,
        };

    private async Task<ProviderHealthCheckResult> SafeCheckHealthAsync(
        IExternalProvider provider,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        try
        {
            return await provider.CheckHealthAsync(tenantId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check threw for provider {Provider} tenant {TenantId}.", provider.Name, tenantId);
            var elapsed = DateTime.UtcNow - started;
            return ProviderHealthCheckResult.Unhealthy(provider.Name, ex.Message, elapsed);
        }
    }

    private static ProviderHealthSummaryDto MapResult(ProviderHealthCheckResult result, ProviderCategory category) =>
        new(
            result.ProviderName,
            category.ToString(),
            result.IsHealthy,
            result.Message,
            (long)result.ResponseTime.TotalMilliseconds,
            result.CheckedAtUtc,
            result.EndpointProbed,
            result.HttpStatusCode);
}

internal static class ProviderCategoryParser
{
    public static bool TryParse(string? raw, out ProviderCategory category)
    {
        category = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return Enum.TryParse(raw, ignoreCase: true, out category) && Enum.IsDefined(typeof(ProviderCategory), category);
    }
}
