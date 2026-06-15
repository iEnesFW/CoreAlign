using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Providers.EFatura.Common;
using Polly;
using Polly.Retry;

namespace CoreAlign.Infrastructure.Providers.EFatura.GibPortal;

public sealed class GibPortalDirectProvider : IEFaturaProvider
{
    public const string HttpClientName = "GibPortalDirect";
    public const string ProviderName = "gib-portal-direct";

    private const string SandboxBaseUrl = "https://earsivportaltest.efatura.gov.tr";
    private const string ProductionBaseUrl = "https://earsivportal.efatura.gov.tr";
    private const string StatusQueryPath = "/earsiv-services/esign";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public GibPortalDirectProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1)));
    }

    public string Name => ProviderName;

    public string DisplayName => "GİB Portal Direct";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.Invoice | ProviderCapability.Archive,
        new Dictionary<string, string>
        {
            ["transport"] = "manual+rest",
            ["auth"] = "smart-card-or-qnb-key",
            ["ubl"] = "UBL-TR 2.1",
            ["canQueryStatus"] = "false",
            ["canCancel"] = "false",
            ["canCreditNote"] = "true",
            ["flow"] = "prepare-xml-only"
        });

    public EFaturaProviderCapabilities SupportedCapabilities =>
        EFaturaProviderCapabilities.CanIssue
        | EFaturaProviderCapabilities.CanCreditNote;

    public object? UnprotectCredentials(IProviderCredentialProtector protector, Guid tenantId, string? encryptedJson)
    {
        ArgumentNullException.ThrowIfNull(protector);
        return protector.UnprotectAs<GibPortalCredentials>(tenantId, ProviderCategory.EFatura, encryptedJson);
    }

    public async Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string endpoint = "/";
        var started = DateTime.UtcNow;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Head, ProductionBaseUrl + endpoint);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var elapsed = DateTime.UtcNow - started;
            return (int)response.StatusCode < 500
                ? ProviderHealthCheckResult.Healthy(Name, elapsed, endpoint, (int)response.StatusCode)
                : ProviderHealthCheckResult.Unhealthy(Name, $"HTTP {(int)response.StatusCode}", elapsed, endpoint, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return ProviderHealthCheckResult.Unhealthy(Name, ex.Message, DateTime.UtcNow - started, endpoint);
        }
    }

    public Task<EFaturaIssueResult> IssueAsync(EFaturaIssueRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uuid = Guid.NewGuid().ToString();
        var xml = UblTrInvoiceBuilder.Build(request.Document, uuid).ToString(SaveOptions.DisableFormatting);
        var fileName = BuildFileName(request.Document.DocumentNumber, uuid);

        var result = new EFaturaIssueResult(
            Uuid: uuid,
            Status: "PreparedForUpload",
            GibStatus: fileName,
            SentAtUtc: DateTime.UtcNow);

        _ = xml;
        return Task.FromResult(result);
    }

    public async Task<EFaturaProviderStatus> GetStatusAsync(EFaturaGetStatusRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Uuid);

        var creds = request.Credentials as GibPortalCredentials;
        var isSandbox = creds?.IsSandbox ?? true;
        var sessionCookie = creds?.SessionCookie;

        if (string.IsNullOrWhiteSpace(sessionCookie))
        {
            return new EFaturaProviderStatus(request.Uuid, "Unknown", null, null);
        }

        try
        {
            var url = BuildBase(isSandbox) + StatusQueryPath;
            var payload = JsonSerializer.Serialize(new { cmd = "EARSIV_PORTAL_FATURA_GETIR", callid = request.Uuid });

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Cookie", sessionCookie!);

            using var response = await ExecuteAsync(httpRequest, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new EFaturaProviderStatus(request.Uuid, "Unknown", null, null);
            }

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var json = JsonDocument.Parse(body);
            var status = json.RootElement.TryGetProperty("status", out var s)
                ? s.GetString() ?? "Unknown"
                : "Unknown";
            var gibCode = json.RootElement.TryGetProperty("gibResponseCode", out var g)
                ? g.GetString()
                : null;

            return new EFaturaProviderStatus(request.Uuid, status, gibCode, DateTime.UtcNow);
        }
        catch (HttpRequestException)
        {
            return new EFaturaProviderStatus(request.Uuid, "Unknown", null, null);
        }
        catch (JsonException)
        {
            return new EFaturaProviderStatus(request.Uuid, "Unknown", null, null);
        }
    }

    public Task<IReadOnlyList<EFaturaInboxItem>> ListReceivedAsync(EFaturaListReceivedRequest request, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<EFaturaInboxItem>>(Array.Empty<EFaturaInboxItem>());

    public Task<EFaturaCancelResult> CancelAsync(EFaturaCancelInvoiceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Uuid);

        return Task.FromResult(new EFaturaCancelResult(
            Ettn: request.Uuid,
            Cancelled: false,
            Reason: "GibPortalDirect cancel must be performed manually via the GİB Portal."));
    }

    public Task<EFaturaCreditNoteResult> CreditNoteAsync(EFaturaCreditNoteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OriginalUuid);

        var uuid = Guid.NewGuid().ToString();

        return Task.FromResult(new EFaturaCreditNoteResult(
            Uuid: uuid,
            Status: "PreparedForUpload",
            IssuedAtUtc: DateTime.UtcNow));
    }

    private async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        return await _retryPolicy.ExecuteAsync(async token =>
        {
            var clone = await CloneAsync(request).ConfigureAwait(false);
            return await client.SendAsync(clone, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private static string BuildBase(bool isSandbox) => isSandbox ? SandboxBaseUrl : ProductionBaseUrl;

    private static string BuildFileName(string documentNumber, string uuid)
    {
        var safeNumber = string.IsNullOrWhiteSpace(documentNumber)
            ? "invoice"
            : new string(documentNumber.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        return $"GIB-{safeNumber}-{uuid}.xml";
    }
}
