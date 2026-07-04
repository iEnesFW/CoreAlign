using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CoreAlign.Infrastructure.Providers.EFatura.Nilvera;

public sealed class NilveraEFaturaProvider : IEFaturaProvider
{
    public const string ProviderKey = "nilvera";
    public const string HttpClientName = "NilveraEFatura";

    private const string SandboxBaseUrl = "https://api-test.nilvera.com";
    private const string ProductionBaseUrl = "https://api.nilvera.com";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantContext _tenantContext;
    private readonly NilveraTokenManager _tokenManager;
    private readonly ILogger<NilveraEFaturaProvider> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public NilveraEFaturaProvider(
        IHttpClientFactory httpClientFactory,
        ITenantContext tenantContext,
        NilveraTokenManager tokenManager,
        ILogger<NilveraEFaturaProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tenantContext = tenantContext;
        _tokenManager = tokenManager;
        _logger = logger;
        _retryPolicy = BuildRetryPolicy();
    }

    public string Name => ProviderKey;

    public string DisplayName => "Nilvera e-Fatura";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.Invoice
            | ProviderCapability.Cancel
            | ProviderCapability.Refund
            | ProviderCapability.Archive
            | ProviderCapability.OAuth
            | ProviderCapability.Webhook
            | ProviderCapability.RealTimeStatus,
        new Dictionary<string, string> { ["transport"] = "rest+oauth2" });

    public EFaturaProviderCapabilities SupportedCapabilities =>
        EFaturaProviderCapabilities.CanIssue
        | EFaturaProviderCapabilities.CanCancel
        | EFaturaProviderCapabilities.CanCreditNote
        | EFaturaProviderCapabilities.CanQueryStatus
        | EFaturaProviderCapabilities.CanListReceived
        | EFaturaProviderCapabilities.CanWebhook
        | EFaturaProviderCapabilities.CanCheckTaxpayer;

    public object? UnprotectCredentials(IProviderCredentialProtector protector, Guid tenantId, string? encryptedJson)
    {
        ArgumentNullException.ThrowIfNull(protector);
        return protector.UnprotectAs<NilveraCredentials>(tenantId, ProviderCategory.EFatura, encryptedJson);
    }

    public async Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string endpoint = "/api/v1/health";
        var started = DateTime.UtcNow;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, SandboxBaseUrl + endpoint);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var elapsed = DateTime.UtcNow - started;
            return response.IsSuccessStatusCode
                ? ProviderHealthCheckResult.Healthy(Name, elapsed, endpoint, (int)response.StatusCode)
                : ProviderHealthCheckResult.Unhealthy(Name, $"HTTP {(int)response.StatusCode}", elapsed, endpoint, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nilvera health check failed for tenant {TenantId}.", tenantId);
            return ProviderHealthCheckResult.Unhealthy(Name, ex.Message, DateTime.UtcNow - started, endpoint);
        }
    }

    public async Task<EFaturaIssueResult> IssueAsync(EFaturaIssueRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ctx = await ResolveContextAsync(request.TenantId, request.Credentials, ct).ConfigureAwait(false);
        var dto = new NilveraInvoiceRequest(
            UblXmlBase64: request.UblXmlBase64,
            CustomerVkn: request.Document.BuyerVkn,
            CustomerTaxOffice: null,
            InvoiceType: request.InvoiceType ?? MapInvoiceType(request.Document.Type),
            Currency: request.Document.Currency);

        var result = await SendJsonAsync<NilveraInvoiceResult>(
            ctx,
            HttpMethod.Post,
            "/api/v1/invoices/sales",
            dto,
            ct).ConfigureAwait(false);

        return new EFaturaIssueResult(result.Uuid, result.Status, result.GibStatus, result.SentAt);
    }

    public async Task<EFaturaCancelResult> CancelAsync(EFaturaCancelInvoiceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Uuid);

        var ctx = await ResolveContextAsync(request.TenantId, request.Credentials, ct).ConfigureAwait(false);
        var dto = new NilveraCancelRequest(request.Reason);

        var result = await SendJsonAsync<NilveraCancelResult>(
            ctx,
            HttpMethod.Post,
            $"/api/v1/invoices/{Uri.EscapeDataString(request.Uuid)}/cancel",
            dto,
            ct).ConfigureAwait(false);

        return new EFaturaCancelResult(result.Uuid, result.Cancelled, request.Reason);
    }

    public async Task<EFaturaProviderStatus> GetStatusAsync(EFaturaGetStatusRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Uuid);

        var ctx = await ResolveContextAsync(request.TenantId, request.Credentials, ct).ConfigureAwait(false);

        var result = await SendJsonAsync<NilveraStatusResult>(
            ctx,
            HttpMethod.Get,
            $"/api/v1/invoices/{Uri.EscapeDataString(request.Uuid)}/status",
            body: null,
            ct).ConfigureAwait(false);

        return new EFaturaProviderStatus(result.Uuid, result.CurrentStatus, result.GibResponseCode, result.DeliveredAt);
    }

    public async Task<IReadOnlyList<EFaturaInboxItem>> ListReceivedAsync(EFaturaListReceivedRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ToUtc < request.FromUtc)
        {
            throw new ArgumentException("ToUtc must be greater than or equal to FromUtc.", nameof(request));
        }

        var ctx = await ResolveContextAsync(request.TenantId, request.Credentials, ct).ConfigureAwait(false);
        var query = $"?fromDate={Uri.EscapeDataString(request.FromUtc.ToString("O"))}&toDate={Uri.EscapeDataString(request.ToUtc.ToString("O"))}";

        var result = await SendJsonAsync<NilveraIncomingListResult>(
            ctx,
            HttpMethod.Get,
            "/api/v1/invoices/incoming" + query,
            body: null,
            ct).ConfigureAwait(false);

        var items = result.Items ?? Array.Empty<NilveraIncomingInvoice>();
        var mapped = new List<EFaturaInboxItem>(items.Count);
        foreach (var item in items)
        {
            mapped.Add(new EFaturaInboxItem(item.Uuid, item.SenderVkn, item.DocumentNumber, item.IssueDate, item.Status));
        }
        return mapped;
    }

    public async Task<EFaturaCreditNoteResult> CreditNoteAsync(EFaturaCreditNoteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var ctx = await ResolveContextAsync(request.TenantId, request.Credentials, ct).ConfigureAwait(false);
        var dto = new NilveraCreditNoteRequest(request.OriginalUuid, request.RefundAmount, request.Currency, request.Reason);

        var result = await SendJsonAsync<NilveraCreditNoteResult>(
            ctx,
            HttpMethod.Post,
            "/api/v1/credit-notes",
            dto,
            ct).ConfigureAwait(false);

        return new EFaturaCreditNoteResult(result.Uuid, result.Status, result.IssuedAt);
    }

    public async Task<EFaturaTaxpayerStatus> CheckTaxpayerAsync(EFaturaTaxpayerCheckRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TaxNumber);

        var ctx = await ResolveContextAsync(request.TenantId, request.Credentials, ct).ConfigureAwait(false);
        try
        {
            var result = await SendJsonAsync<NilveraTaxpayerResult>(
                ctx,
                HttpMethod.Get,
                $"/api/v1/taxpayers/{Uri.EscapeDataString(request.TaxNumber)}",
                body: null,
                ct).ConfigureAwait(false);

            return new EFaturaTaxpayerStatus(request.TaxNumber, result.IsRegistered, result.Alias, result.Title);
        }
        catch (NilveraProviderException ex) when (ex.ErrorCode is "HTTP_404" or "TAXPAYER_NOT_FOUND")
        {
            return new EFaturaTaxpayerStatus(request.TaxNumber, IsEFaturaRegistered: false);
        }
    }

    private async Task<NilveraInvocationContext> ResolveContextAsync(Guid requestTenantId, object? requestCredentials, CancellationToken ct)
    {
        var tenantId = requestTenantId == Guid.Empty ? _tenantContext.RequireTenantId() : requestTenantId;

        var credentials = requestCredentials as NilveraCredentials
            ?? throw new NilveraProviderException("CREDENTIALS_MISSING", "Nilvera credentials are not configured for the current tenant.");

        var baseUrl = credentials.IsSandbox ? SandboxBaseUrl : ProductionBaseUrl;
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        if (httpClient.BaseAddress is null)
        {
            httpClient.BaseAddress = new Uri(baseUrl);
        }

        var accessToken = await _tokenManager.GetAccessTokenAsync(httpClient, tenantId, credentials, baseUrl, ct).ConfigureAwait(false);
        return new NilveraInvocationContext(tenantId, credentials, baseUrl, httpClient, accessToken);
    }

    private async Task<T> SendJsonAsync<T>(
        NilveraInvocationContext ctx,
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken ct)
        where T : class
    {
        var token = ctx.AccessToken;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await _retryPolicy.ExecuteAsync(async pollyCt =>
            {
                using var request = BuildRequest(method, ctx.BaseUrl + relativePath, body, token);
                return await ctx.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, pollyCt).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            try
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("Nilvera returned 401; refreshing access token for tenant {TenantId}.", ctx.TenantId);
                    token = await _tokenManager.RefreshTokenAsync(ctx.HttpClient, ctx.TenantId, ctx.Credentials, ctx.BaseUrl, refreshToken: null, ct).ConfigureAwait(false);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    throw NilveraProviderException.FromBody((int)response.StatusCode, raw);
                }

                var parsed = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
                return parsed ?? throw new NilveraProviderException("RESPONSE_EMPTY", "Nilvera response body was empty.");
            }
            finally
            {
                response.Dispose();
            }
        }

        throw new NilveraProviderException("AUTH_RETRY_EXHAUSTED", "Nilvera authentication failed after refresh attempt.");
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, object? body, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        return request;
    }

    private AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy() =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .OrResult(static r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: static attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                onRetry: (outcome, delay, attempt, _) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Nilvera HTTP attempt {Attempt} failed (status {Status}); retrying in {Delay}.",
                        attempt,
                        outcome.Result?.StatusCode,
                        delay);
                });

    private static string MapInvoiceType(EFaturaDocumentType type) => type switch
    {
        EFaturaDocumentType.Invoice => "SATIS",
        EFaturaDocumentType.Despatch => "IRSALIYE",
        EFaturaDocumentType.ProducerReceipt => "MUSTAHSIL",
        EFaturaDocumentType.EArchive => "EARSIV",
        EFaturaDocumentType.SelfEmployedReceipt => "SMM",
        _ => "SATIS",
    };

    private sealed record NilveraInvocationContext(
        Guid TenantId,
        NilveraCredentials Credentials,
        string BaseUrl,
        HttpClient HttpClient,
        string AccessToken);
}
