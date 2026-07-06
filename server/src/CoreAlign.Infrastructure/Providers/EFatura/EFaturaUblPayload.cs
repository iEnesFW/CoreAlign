using System.Text;
using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Infrastructure.Providers.EFatura;

// Chooses the base64 payload sent to the e-Fatura/e-İrsaliye provider: the full UBL-TR XML
// produced upstream (invoice or DespatchAdvice) when present, otherwise a minimal
// document-number stub kept only as a defensive fallback for callers that build an
// EFaturaDocument without the raw XML. Historically the dispatcher ALWAYS sent the stub, so
// lines/tax/totals/carrier never reached the provider.
public static class EFaturaUblPayload
{
    public static string ToBase64(EFaturaDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var raw = !string.IsNullOrWhiteSpace(document.RawUblTrXml)
            ? document.RawUblTrXml!
            : $"<Invoice><DocumentNumber>{document.DocumentNumber}</DocumentNumber></Invoice>";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }
}
