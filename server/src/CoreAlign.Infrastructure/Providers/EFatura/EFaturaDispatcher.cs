using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.Common.Outbox;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Application.Providers.EFatura.Events;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Providers.EFatura;

public sealed class EFaturaDispatcher : IEFaturaDispatcher
{
    private const string DispatchAttemptedMessageType = "EFaturaDispatchAttempted";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IProviderRegistry<IEFaturaProvider> _registry;
    private readonly ITenantProviderConfigRepository _configRepository;
    private readonly IProviderCredentialProtector _credentialProtector;
    private readonly ITenantContext _tenantContext;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<EFaturaDispatcher> _logger;

    public EFaturaDispatcher(
        IProviderRegistry<IEFaturaProvider> registry,
        ITenantProviderConfigRepository configRepository,
        IProviderCredentialProtector credentialProtector,
        ITenantContext tenantContext,
        IOutboxRepository outboxRepository,
        IOutboxSignal outboxSignal,
        ILogger<EFaturaDispatcher> logger)
    {
        _registry = registry;
        _configRepository = configRepository;
        _credentialProtector = credentialProtector;
        _tenantContext = tenantContext;
        _outboxRepository = outboxRepository;
        _outboxSignal = outboxSignal;
        _logger = logger;
    }

    public async Task<EFaturaDispatchResult> SubmitAsync(EFaturaDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var tenantId = _tenantContext.RequireTenantId();
        var resolved = await ResolveOrderedProvidersAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (resolved.Count == 0)
        {
            throw new ProviderNotConfiguredException(ProviderCategory.EFatura, tenantId);
        }

        var attempts = new List<EFaturaAttemptInfo>(resolved.Count);
        Exception? lastTransient = null;

        for (var i = 0; i < resolved.Count; i++)
        {
            var entry = resolved[i];
            var startedAt = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var creds = entry.Provider.UnprotectCredentials(_credentialProtector, tenantId, entry.Config?.EncryptedCredentialsJson);
                var ublXmlBase64 = EFaturaUblPayload.ToBase64(document);
                var request = new EFaturaIssueRequest(
                    Document: document,
                    UblXmlBase64: ublXmlBase64,
                    InvoiceType: null,
                    TenantId: tenantId,
                    Credentials: creds);

                var issueResult = await entry.Provider.IssueAsync(request, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                var submitResult = new EFaturaSubmitResult(
                    Ettn: issueResult.Uuid,
                    Status: issueResult.Status,
                    ProviderRefId: issueResult.GibStatus ?? issueResult.Uuid,
                    SubmittedAtUtc: issueResult.SentAtUtc);

                var attempt = new EFaturaAttemptInfo(entry.Provider.Name, true, null, startedAt, stopwatch.Elapsed);
                attempts.Add(attempt);
                await RaiseAttemptEventAsync(tenantId, document, attempt, cancellationToken).ConfigureAwait(false);

                return new EFaturaDispatchResult(
                    submitResult,
                    entry.Provider.Name,
                    FailoverOccurred: attempts.Count > 1,
                    attempts);
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                stopwatch.Stop();
                lastTransient = ex;
                var attempt = new EFaturaAttemptInfo(entry.Provider.Name, false, ex.GetBaseException().Message, startedAt, stopwatch.Elapsed);
                attempts.Add(attempt);
                await RaiseAttemptEventAsync(tenantId, document, attempt, cancellationToken).ConfigureAwait(false);
                _logger.LogWarning(
                    ex,
                    "EFatura provider {Provider} transient failure for tenant {TenantId}; attempting failover.",
                    entry.Provider.Name,
                    tenantId);
                continue;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var attempt = new EFaturaAttemptInfo(entry.Provider.Name, false, ex.GetBaseException().Message, startedAt, stopwatch.Elapsed);
                attempts.Add(attempt);
                await RaiseAttemptEventAsync(tenantId, document, attempt, cancellationToken).ConfigureAwait(false);
                _logger.LogError(
                    ex,
                    "EFatura provider {Provider} permanent failure for tenant {TenantId}; aborting dispatch.",
                    entry.Provider.Name,
                    tenantId);
                throw;
            }
        }

        throw new AllProvidersFailedException(
            $"All EFatura providers failed for tenant {tenantId}. Attempts: {attempts.Count}.",
            attempts,
            lastTransient ?? new InvalidOperationException("No transient error captured."));
    }

    public async Task<EFaturaStatus> GetStatusAsync(string ettn, string? providerNameOverride = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);

        var tenantId = _tenantContext.RequireTenantId();
        var entry = await ResolveSingleProviderAsync(tenantId, providerNameOverride, cancellationToken).ConfigureAwait(false);

        var creds = entry.Provider.UnprotectCredentials(_credentialProtector, tenantId, entry.Config?.EncryptedCredentialsJson);
        var request = new EFaturaGetStatusRequest(ettn, tenantId, creds);
        var providerStatus = await entry.Provider.GetStatusAsync(request, cancellationToken).ConfigureAwait(false);
        return new EFaturaStatus(providerStatus.Uuid, providerStatus.CurrentStatus, providerStatus.DeliveredAtUtc ?? DateTime.UtcNow);
    }

    public async Task<EFaturaCancelResult> CancelAsync(string ettn, string reason, string? providerNameOverride = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);

        var tenantId = _tenantContext.RequireTenantId();
        var entry = await ResolveSingleProviderAsync(tenantId, providerNameOverride, cancellationToken).ConfigureAwait(false);

        var creds = entry.Provider.UnprotectCredentials(_credentialProtector, tenantId, entry.Config?.EncryptedCredentialsJson);
        var request = new EFaturaCancelInvoiceRequest(ettn, reason ?? string.Empty, tenantId, creds);
        return await entry.Provider.CancelAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<EFaturaTaxpayerStatus> CheckTaxpayerAsync(string taxNumber, string? providerNameOverride = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taxNumber);

        var tenantId = _tenantContext.RequireTenantId();
        var entry = await ResolveSingleProviderAsync(tenantId, providerNameOverride, cancellationToken).ConfigureAwait(false);

        var creds = entry.Provider.UnprotectCredentials(_credentialProtector, tenantId, entry.Config?.EncryptedCredentialsJson);
        var request = new EFaturaTaxpayerCheckRequest(taxNumber, tenantId, creds);
        return await entry.Provider.CheckTaxpayerAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EFaturaInboxItem>> ListReceivedAsync(DateTime fromUtc, DateTime toUtc, string? providerNameOverride = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var entry = await ResolveSingleProviderAsync(tenantId, providerNameOverride, cancellationToken).ConfigureAwait(false);

        var creds = entry.Provider.UnprotectCredentials(_credentialProtector, tenantId, entry.Config?.EncryptedCredentialsJson);
        var request = new EFaturaListReceivedRequest(fromUtc, toUtc, tenantId, creds);
        return await entry.Provider.ListReceivedAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ResolvedProvider>> ResolveOrderedProvidersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var configs = await _configRepository
            .ListByTenantAsync(tenantId, ProviderCategory.EFatura, cancellationToken)
            .ConfigureAwait(false);

        var ordered = configs
            .Where(c => c.IsEnabled)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resolved = new List<ResolvedProvider>(ordered.Count);
        foreach (var config in ordered)
        {
            var provider = _registry.Find(config.ProviderName);
            if (provider is null)
            {
                _logger.LogWarning(
                    "EFatura provider {Provider} configured for tenant {TenantId} is not registered; skipping.",
                    config.ProviderName,
                    tenantId);
                continue;
            }
            resolved.Add(new ResolvedProvider(provider, config));
        }
        return resolved;
    }

    private async Task<ResolvedProvider> ResolveSingleProviderAsync(Guid tenantId, string? providerNameOverride, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(providerNameOverride))
        {
            var explicitProvider = _registry.Require(providerNameOverride!);
            var explicitConfig = await _configRepository
                .GetByTenantAndCategoryAsync(tenantId, ProviderCategory.EFatura, providerNameOverride!, cancellationToken)
                .ConfigureAwait(false);
            return new ResolvedProvider(explicitProvider, explicitConfig);
        }

        var defaultConfig = await _configRepository
            .GetDefaultForTenantAsync(tenantId, ProviderCategory.EFatura, cancellationToken)
            .ConfigureAwait(false);
        if (defaultConfig is null)
        {
            var fallbackProvider = await _registry.ResolveForTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
            return new ResolvedProvider(fallbackProvider, null);
        }

        var defaultProvider = _registry.Require(defaultConfig.ProviderName);
        return new ResolvedProvider(defaultProvider, defaultConfig);
    }


    private async Task RaiseAttemptEventAsync(
        Guid tenantId,
        EFaturaDocument document,
        EFaturaAttemptInfo attempt,
        CancellationToken cancellationToken)
    {
        try
        {
            var evt = new EFaturaDispatchAttemptedEvent(
                tenantId,
                attempt.ProviderName,
                document.DocumentNumber,
                attempt.Succeeded,
                attempt.ErrorMessage,
                attempt.AttemptedAtUtc,
                attempt.Duration);

            var payload = JsonSerializer.Serialize(evt, JsonOptions);
            await _outboxRepository
                .AddAsync(new OutboxMessage(DispatchAttemptedMessageType, payload), cancellationToken)
                .ConfigureAwait(false);
            _outboxSignal.MarkPending();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to enqueue EFaturaDispatchAttempted event for provider {Provider}; continuing dispatch.",
                attempt.ProviderName);
        }
    }

    private static bool IsTransient(Exception ex) => ex switch
    {
        HttpRequestException => true,
        TaskCanceledException tex when !tex.CancellationToken.IsCancellationRequested => true,
        TimeoutException => true,
        _ => HasTransientStatusCode(ex),
    };

    private static bool HasTransientStatusCode(Exception ex)
    {
        if (ex is HttpRequestException httpEx && httpEx.StatusCode is { } status)
        {
            var code = (int)status;
            return code == 408 || code == 429 || code >= 500;
        }
        return false;
    }

    private sealed record ResolvedProvider(IEFaturaProvider Provider, TenantProviderConfig? Config);
}
