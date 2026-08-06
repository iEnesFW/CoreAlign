using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CoreAlign.Application.EInvoice;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Domain.Enums;
using Polly;
using Polly.Retry;

namespace CoreAlign.Infrastructure.Providers.EFatura.Foriba;

public sealed class ForibaEFaturaProvider : IEFaturaProvider
{
    public const string HttpClientName = "ForibaEFatura";
    public const string ProviderName = "foriba";

    private const string SandboxBase = "https://ws-test.foriba.com.tr";
    private const string ProductionBase = "https://ws.foriba.com.tr";
    private const string SoapEndpointPath = "/efatura/services/EInvoiceService";
    private const string RestStatusPath = "/efatura/services/rest/v1/status/";
    private const string RestInboxPath = "/efatura/services/rest/v1/inbox";

    private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace ForibaNs = "http://foriba.com.tr/efatura";
    private static readonly XNamespace UblInvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace CbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace CacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public ForibaEFaturaProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1)));
    }

    public string Name => ProviderName;

    public string DisplayName => "Foriba e-Fatura";

    public ProviderCapabilities Capabilities => new(
        ProviderCapability.Invoice
            | ProviderCapability.Despatch
            | ProviderCapability.Cancel
            | ProviderCapability.Archive
            | ProviderCapability.WebhookCallback
            | ProviderCapability.SignatureValidation,
        new Dictionary<string, string>
        {
            ["transport"] = "soap+rest",
            ["auth"] = "ws-security-username-token",
            ["ubl"] = "UBL-TR 2.1"
        });

    public EFaturaProviderCapabilities SupportedCapabilities =>
        EFaturaProviderCapabilities.CanIssue
        | EFaturaProviderCapabilities.CanCancel
        | EFaturaProviderCapabilities.CanCreditNote
        | EFaturaProviderCapabilities.CanQueryStatus
        | EFaturaProviderCapabilities.CanListReceived
        | EFaturaProviderCapabilities.CanWebhook;

    public object? UnprotectCredentials(IProviderCredentialProtector protector, Guid tenantId, string? encryptedJson)
    {
        ArgumentNullException.ThrowIfNull(protector);
        return protector.UnprotectAs<ForibaCredentials>(tenantId, ProviderCategory.EFatura, encryptedJson);
    }

    public async Task<ProviderHealthCheckResult> CheckHealthAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        const string endpoint = "/efatura/services/rest/v1/health";
        var started = DateTime.UtcNow;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, SandboxBase + endpoint);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var elapsed = DateTime.UtcNow - started;
            return response.IsSuccessStatusCode
                ? ProviderHealthCheckResult.Healthy(Name, elapsed, endpoint, (int)response.StatusCode)
                : ProviderHealthCheckResult.Unhealthy(Name, $"HTTP {(int)response.StatusCode}", elapsed, endpoint, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return ProviderHealthCheckResult.Unhealthy(Name, ex.Message, DateTime.UtcNow - started, endpoint);
        }
    }

    public async Task<EFaturaIssueResult> IssueAsync(EFaturaIssueRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var creds = request.Credentials as ForibaCredentials
            ?? throw new InvalidOperationException("Foriba credentials are not configured for the current tenant.");

        var xml = BuildUblXml(request.Document);

        var soap = ForibaSoapEnvelopeBuilder.BuildInvoiceSubmit(
            creds.Username,
            creds.Password,
            action: "submit",
            documentUuid: xml.DocumentUuid,
            ublXmlBody: xml.XmlPayload,
            utcNow: DateTime.UtcNow);

        using var response = await SendSoapAsync(soap, "submitInvoice", creds.IsSandbox, ct).ConfigureAwait(false);
        var body = await ReadAndEnsureAsync(response, ct).ConfigureAwait(false);
        var result = ParseInvoiceResult(body);

        return new EFaturaIssueResult(
            Uuid: result.Uuid,
            Status: result.Status,
            GibStatus: string.IsNullOrWhiteSpace(result.GibResponseCode) ? null : result.GibResponseCode,
            SentAtUtc: DateTime.UtcNow);
    }

    public async Task<EFaturaProviderStatus> GetStatusAsync(EFaturaGetStatusRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Uuid);

        var creds = request.Credentials as ForibaCredentials
            ?? throw new InvalidOperationException("Foriba credentials are not configured for the current tenant.");

        var url = BuildBase(creds.IsSandbox) + RestStatusPath + Uri.EscapeDataString(request.Uuid);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyBasicAuth(httpRequest, creds);

        using var response = await SendRestAsync(httpRequest, ct).ConfigureAwait(false);
        var body = await ReadAndEnsureAsync(response, ct).ConfigureAwait(false);

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "Unknown" : "Unknown";
        var gibCode = root.TryGetProperty("gibResponseCode", out var g) ? g.GetString() : null;

        return new EFaturaProviderStatus(request.Uuid, status, gibCode, DateTime.UtcNow);
    }

    public async Task<EFaturaCancelResult> CancelAsync(EFaturaCancelInvoiceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Uuid);

        var creds = request.Credentials as ForibaCredentials
            ?? throw new InvalidOperationException("Foriba credentials are not configured for the current tenant.");

        var soap = ForibaSoapEnvelopeBuilder.BuildCancelRequest(
            creds.Username,
            creds.Password,
            request.Uuid,
            request.Reason ?? string.Empty,
            DateTime.UtcNow);

        using var response = await SendSoapAsync(soap, "cancelInvoice", creds.IsSandbox, ct).ConfigureAwait(false);
        var body = await ReadAndEnsureAsync(response, ct).ConfigureAwait(false);

        var doc = XDocument.Parse(body);
        var cancelled = doc.Descendants(ForibaNs + "Cancelled").FirstOrDefault()?.Value;
        var success = string.Equals(cancelled, "true", StringComparison.OrdinalIgnoreCase);

        return new EFaturaCancelResult(request.Uuid, success, request.Reason ?? string.Empty);
    }

    public async Task<IReadOnlyList<EFaturaInboxItem>> ListReceivedAsync(EFaturaListReceivedRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var creds = request.Credentials as ForibaCredentials
            ?? throw new InvalidOperationException("Foriba credentials are not configured for the current tenant.");

        var query = string.Format(
            CultureInfo.InvariantCulture,
            "?from={0}&to={1}",
            request.FromUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            request.ToUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildBase(creds.IsSandbox) + RestInboxPath + query);
        ApplyBasicAuth(httpRequest, creds);

        using var response = await SendRestAsync(httpRequest, ct).ConfigureAwait(false);
        var body = await ReadAndEnsureAsync(response, ct).ConfigureAwait(false);

        var items = new List<EFaturaInboxItem>();
        using var json = JsonDocument.Parse(body);

        if (!json.RootElement.TryGetProperty("items", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return items;
        }

        foreach (var element in arr.EnumerateArray())
        {
            items.Add(new EFaturaInboxItem(
                Uuid: element.TryGetProperty("uuid", out var uuid) ? uuid.GetString() ?? string.Empty : string.Empty,
                SenderVkn: element.TryGetProperty("senderVkn", out var vkn) ? vkn.GetString() ?? string.Empty : string.Empty,
                DocumentNumber: element.TryGetProperty("documentNumber", out var dn) ? dn.GetString() ?? string.Empty : string.Empty,
                IssueDate: element.TryGetProperty("issueDate", out var date) && date.TryGetDateTime(out var parsed) ? parsed : default,
                Status: element.TryGetProperty("status", out var st) ? st.GetString() ?? string.Empty : string.Empty));
        }

        return items;
    }

    public async Task<EFaturaCreditNoteResult> CreditNoteAsync(EFaturaCreditNoteRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OriginalUuid);

        var creds = request.Credentials as ForibaCredentials
            ?? throw new InvalidOperationException("Foriba credentials are not configured for the current tenant.");

        var creditDoc = new EFaturaDocument(
            Type: EFaturaDocumentType.Invoice,
            DocumentNumber: $"CN-{request.OriginalUuid}",
            IssueDate: DateTime.UtcNow,
            BuyerVkn: string.Empty,
            BuyerName: string.Empty,
            Lines: Array.Empty<EFaturaLine>(),
            Currency: request.Currency,
            TotalAmount: request.RefundAmount);
        var xml = BuildUblXml(creditDoc);

        var soap = ForibaSoapEnvelopeBuilder.BuildCreditNoteRequest(
            creds.Username,
            creds.Password,
            request.OriginalUuid,
            xml.XmlPayload,
            DateTime.UtcNow);

        using var response = await SendSoapAsync(soap, "creditNote", creds.IsSandbox, ct).ConfigureAwait(false);
        var body = await ReadAndEnsureAsync(response, ct).ConfigureAwait(false);
        var result = ParseInvoiceResult(body);

        return new EFaturaCreditNoteResult(result.Uuid, result.Status, DateTime.UtcNow);
    }

    private static EFaturaXmlBuildResult BuildUblXml(EFaturaDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);

        var uuid = Guid.NewGuid().ToString();
        var profileId = doc.Type switch
        {
            EFaturaDocumentType.EArchive => "EARSIVFATURA",
            EFaturaDocumentType.Despatch => "TEMELIRSALIYE",
            _ => "TICARIFATURA"
        };

        var taxableTotal = doc.Lines.Sum(l => l.Quantity * l.UnitPrice);
        var taxTotal = doc.Lines.Sum(l => l.Quantity * l.UnitPrice * (l.VatRate / 100m));
        var payable = taxableTotal + taxTotal;

        var invoice = new XElement(UblInvoiceNs + "Invoice",
            new XAttribute(XNamespace.Xmlns + "cbc", CbcNs.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "cac", CacNs.NamespaceName),
            new XElement(CbcNs + "UBLVersionID", "2.1"),
            new XElement(CbcNs + "CustomizationID", "TR1.2"),
            new XElement(CbcNs + "ProfileID", profileId),
            new XElement(CbcNs + "ID", doc.DocumentNumber),
            new XElement(CbcNs + "UUID", uuid),
            new XElement(CbcNs + "IssueDate", doc.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "InvoiceTypeCode", "SATIS"),
            new XElement(CbcNs + "DocumentCurrencyCode", doc.Currency),
            BuildPartyElement("AccountingCustomerParty", doc.BuyerVkn, doc.BuyerName),
            BuildTaxTotal(taxTotal, doc.Currency),
            BuildLegalMonetaryTotal(taxableTotal, payable, doc.Currency),
            BuildInvoiceLines(doc));

        var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), invoice)
            .ToString(SaveOptions.DisableFormatting);

        return new EFaturaXmlBuildResult(xml, profileId, uuid);
    }

    private static XElement BuildPartyElement(string role, string vkn, string name) =>
        new(CacNs + role,
            new XElement(CacNs + "Party",
                new XElement(CacNs + "PartyIdentification",
                    new XElement(CbcNs + "ID",
                        new XAttribute("schemeID", vkn.Length == 11 ? "TCKN" : "VKN"),
                        vkn)),
                new XElement(CacNs + "PartyName",
                    new XElement(CbcNs + "Name", name))));

    private static XElement BuildTaxTotal(decimal taxAmount, string currency) =>
        new(CacNs + "TaxTotal",
            new XElement(CbcNs + "TaxAmount",
                new XAttribute("currencyID", currency),
                taxAmount.ToString("F2", CultureInfo.InvariantCulture)));

    private static XElement BuildLegalMonetaryTotal(decimal taxable, decimal payable, string currency) =>
        new(CacNs + "LegalMonetaryTotal",
            new XElement(CbcNs + "LineExtensionAmount",
                new XAttribute("currencyID", currency),
                taxable.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "TaxExclusiveAmount",
                new XAttribute("currencyID", currency),
                taxable.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "TaxInclusiveAmount",
                new XAttribute("currencyID", currency),
                payable.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(CbcNs + "PayableAmount",
                new XAttribute("currencyID", currency),
                payable.ToString("F2", CultureInfo.InvariantCulture)));

    private static IEnumerable<XElement> BuildInvoiceLines(EFaturaDocument doc)
    {
        var index = 1;
        foreach (var line in doc.Lines)
        {
            var lineTotal = line.Quantity * line.UnitPrice;
            yield return new XElement(CacNs + "InvoiceLine",
                new XElement(CbcNs + "ID", index.ToString(CultureInfo.InvariantCulture)),
                new XElement(CbcNs + "InvoicedQuantity",
                    new XAttribute("unitCode", GibUnitCodeMap.DefaultCode),
                    line.Quantity.ToString("F3", CultureInfo.InvariantCulture)),
                new XElement(CbcNs + "LineExtensionAmount",
                    new XAttribute("currencyID", doc.Currency),
                    lineTotal.ToString("F2", CultureInfo.InvariantCulture)),
                new XElement(CacNs + "Item",
                    new XElement(CbcNs + "Name", line.Name)),
                new XElement(CacNs + "Price",
                    new XElement(CbcNs + "PriceAmount",
                        new XAttribute("currencyID", doc.Currency),
                        line.UnitPrice.ToString("F4", CultureInfo.InvariantCulture))));
            index++;
        }
    }

    private static string BuildBase(bool isSandbox) => isSandbox ? SandboxBase : ProductionBase;

    private static void ApplyBasicAuth(HttpRequestMessage request, ForibaCredentials creds)
    {
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{creds.Username}:{creds.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<HttpResponseMessage> SendSoapAsync(string soapBody, string soapAction, bool isSandbox, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var url = BuildBase(isSandbox) + SoapEndpointPath;

        return await _retryPolicy.ExecuteAsync(async token =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(soapBody, Encoding.UTF8, "text/xml")
            };
            request.Headers.Add("SOAPAction", $"\"{soapAction}\"");
            return await client.SendAsync(request, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendRestAsync(HttpRequestMessage request, CancellationToken ct)
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

    private static async Task<string> ReadAndEnsureAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Foriba request failed with status {(int)response.StatusCode}: {body}",
                inner: null,
                statusCode: response.StatusCode);
        }
        return body;
    }

    private static ForibaInvoiceResult ParseInvoiceResult(string soapBody)
    {
        var doc = XDocument.Parse(soapBody);
        var fault = doc.Descendants(SoapNs + "Fault").FirstOrDefault();
        if (fault != null)
        {
            var faultString = fault.Element("faultstring")?.Value ?? "Unknown SOAP fault";
            throw new InvalidOperationException($"Foriba SOAP fault: {faultString}");
        }

        var uuid = doc.Descendants(ForibaNs + "Uuid").FirstOrDefault()?.Value ?? string.Empty;
        var status = doc.Descendants(ForibaNs + "Status").FirstOrDefault()?.Value ?? "Unknown";
        var gibCode = doc.Descendants(ForibaNs + "GibResponseCode").FirstOrDefault()?.Value ?? string.Empty;
        var refId = doc.Descendants(ForibaNs + "ProviderRefId").FirstOrDefault()?.Value;

        return new ForibaInvoiceResult(uuid, status, gibCode, refId);
    }
}
