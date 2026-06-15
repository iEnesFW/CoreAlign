using System.Diagnostics;
using System.Text.Json;
using Asp.Versioning;
using CoreAlign.API.Common;
using CoreAlign.Application.Authorization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.Admin;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Application.Providers.Payment;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreAlign.API.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AdminPolicies.ProviderConfig)]
[Route("api/v{version:apiVersion}/admin/providers")]
public class ProviderTestRunnerController : ControllerBase
{
    private const string SandboxKey = "isSandbox";
    private const string SandboxKeyAlt = "IsSandbox";
    private const string SandboxKeyLegacy = "sandbox";

    private readonly IProviderRegistry<IEFaturaProvider> _eFaturaRegistry;
    private readonly IProviderRegistry<IPaymentProvider> _paymentRegistry;
    private readonly ITenantProviderConfigRepository _configRepository;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProviderTestRunnerController> _logger;

    public ProviderTestRunnerController(
        IProviderRegistry<IEFaturaProvider> eFaturaRegistry,
        IProviderRegistry<IPaymentProvider> paymentRegistry,
        ITenantProviderConfigRepository configRepository,
        IProviderCredentialProtector credentialProtector,
        ITenantContext tenantContext,
        ILogger<ProviderTestRunnerController> logger)
    {
        _eFaturaRegistry = eFaturaRegistry;
        _paymentRegistry = paymentRegistry;
        _configRepository = configRepository;
        _credentialProtector = credentialProtector;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("{category}/{providerName}/test-suite")]
    public async Task<IActionResult> RunTestSuite(
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
        var config = await _configRepository.GetByTenantAndCategoryAsync(tenantId, parsedCategory, providerName, cancellationToken);
        if (config is null || !config.IsEnabled)
        {
            return BadRequest(ApiResponse<object>.Failure(
                $"Provider '{providerName}' for category '{parsedCategory}' is not configured or disabled for this tenant.",
                400));
        }

        var isSandbox = IsSandboxConfigured(tenantId, parsedCategory, config.EncryptedCredentialsJson);
        if (!isSandbox)
        {
            return StatusCode(StatusCodes.Status409Conflict,
                ApiResponse<object>.Failure(
                    "Test suite refused: provider is not in sandbox mode. Production credentials cannot run the test harness.",
                    409));
        }

        var started = DateTime.UtcNow;
        var steps = new List<ProviderTestRunStepResult>();

        switch (parsedCategory)
        {
            case ProviderCategory.EFatura:
                {
                    var provider = _eFaturaRegistry.Find(providerName);
                    if (provider is null)
                    {
                        return NotFound(ApiResponse<object>.Failure($"e-Fatura provider '{providerName}' is not registered.", 404));
                    }

                    steps.AddRange(await RunEFaturaTestSuiteAsync(provider, tenantId, cancellationToken));
                    break;
                }
            case ProviderCategory.Payment:
                {
                    var provider = _paymentRegistry.Find(providerName);
                    if (provider is null)
                    {
                        return NotFound(ApiResponse<object>.Failure($"Payment provider '{providerName}' is not registered.", 404));
                    }

                    steps.AddRange(await RunPaymentTestSuiteAsync(provider, tenantId, cancellationToken));
                    break;
                }
            default:
                return BadRequest(ApiResponse<object>.Failure(
                    $"Test suite is not implemented for category '{parsedCategory}'.",
                    400));
        }

        var completed = DateTime.UtcNow;
        var result = new ProviderTestRunResultDto(
            providerName,
            parsedCategory.ToString(),
            Sandbox: true,
            AllPassed: steps.All(s => s.Passed),
            StartedAtUtc: started,
            CompletedAtUtc: completed,
            Steps: steps);

        return Ok(ApiResponse<ProviderTestRunResultDto>.Success(result));
    }

    private async Task<IReadOnlyList<ProviderTestRunStepResult>> RunEFaturaTestSuiteAsync(
        IEFaturaProvider provider,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var steps = new List<ProviderTestRunStepResult>();

        steps.Add(await TimedStepAsync("Health", async () =>
        {
            var health = await provider.CheckHealthAsync(tenantId, cancellationToken);
            return (health.IsHealthy, health.Message);
        }));

        steps.Add(await TimedStepAsync("CapabilityAdvertised", () =>
        {
            var advertises = provider.SupportedCapabilities.HasFlag(EFaturaProviderCapabilities.CanIssue);
            return Task.FromResult((advertises, advertises ? null : "Provider does not advertise CanIssue capability."));
        }));

        steps.Add(await TimedStepAsync("DummyInvoiceIssue", async () =>
        {
            var doc = BuildSandboxInvoice();
            var ublXml = $"<Invoice><DocumentNumber>{doc.DocumentNumber}</DocumentNumber></Invoice>";
            var ublXmlBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ublXml));
            var request = new EFaturaIssueRequest(doc, ublXmlBase64, "STANDARD");

            try
            {
                var issueResult = await provider.IssueAsync(request, cancellationToken);
                return (!string.IsNullOrWhiteSpace(issueResult.Uuid),
                        $"uuid={issueResult.Uuid}, status={issueResult.Status}");
            }
            catch (NotSupportedException nex)
            {
                return (false, $"Provider does not support IssueAsync: {nex.Message}");
            }
        }));

        return steps;
    }

    private async Task<IReadOnlyList<ProviderTestRunStepResult>> RunPaymentTestSuiteAsync(
        IPaymentProvider provider,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var steps = new List<ProviderTestRunStepResult>();

        steps.Add(await TimedStepAsync("Health", async () =>
        {
            var health = await provider.CheckHealthAsync(tenantId, cancellationToken);
            return (health.IsHealthy, health.Message);
        }));

        steps.Add(await TimedStepAsync("ListMethods", async () =>
        {
            try
            {
                var methods = await provider.ListMethodsAsync(tenantId, cancellationToken);
                return (methods.Count > 0, $"methodCount={methods.Count}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }));

        steps.Add(await TimedStepAsync("SandboxChargeOneTry", async () =>
        {
            try
            {
                var intent = new PaymentIntentRequest(
                    Amount: 1m,
                    Currency: "TRY",
                    OrderReference: $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    BuyerName: "Test Buyer",
                    BuyerEmail: "test@corealign.local");

                var link = await provider.CreateLinkAsync(
                    intent,
                    new PaymentLinkOptions(ExpiryMinutes: 5, CallbackUrl: "https://localhost/test-callback"),
                    cancellationToken);
                return (!string.IsNullOrWhiteSpace(link.LinkUrl), $"providerRef={link.ProviderRefId}");
            }
            catch (NotSupportedException nex)
            {
                return (false, $"Provider does not support CreateLinkAsync: {nex.Message}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }));

        return steps;
    }

    private async Task<ProviderTestRunStepResult> TimedStepAsync(
        string name,
        Func<Task<(bool Passed, string? Detail)>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var (passed, detail) = await action();
            sw.Stop();
            return new ProviderTestRunStepResult(name, passed, detail, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Test step {Step} threw.", name);
            return new ProviderTestRunStepResult(name, false, ex.Message, sw.ElapsedMilliseconds);
        }
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
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var key in new[] { SandboxKey, SandboxKeyAlt, SandboxKeyLegacy })
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

    private static EFaturaDocument BuildSandboxInvoice() => new(
        Type: EFaturaDocumentType.Invoice,
        DocumentNumber: $"TEST-{DateTime.UtcNow:yyyyMMddHHmmss}",
        IssueDate: DateTime.UtcNow.Date,
        BuyerVkn: "1234567890",
        BuyerName: "Test Buyer",
        Lines: new[] { new EFaturaLine(Quantity: 1m, Name: "Sandbox Test Item", UnitPrice: 1m, VatRate: 0m) },
        Currency: "TRY",
        TotalAmount: 1m);
}
