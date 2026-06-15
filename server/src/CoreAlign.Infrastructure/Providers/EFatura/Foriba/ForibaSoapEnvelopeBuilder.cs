using System.Globalization;
using System.Xml.Linq;

namespace CoreAlign.Infrastructure.Providers.EFatura.Foriba;

public static class ForibaSoapEnvelopeBuilder
{
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private static readonly XNamespace Wsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private static readonly XNamespace Foriba = "http://foriba.com.tr/efatura";

    private const string PasswordTextType =
        "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";

    public static string BuildInvoiceSubmit(
        string username,
        string password,
        string action,
        string documentUuid,
        string ublXmlBody,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(ublXmlBody);

        var ublElement = XElement.Parse(ublXmlBody, LoadOptions.PreserveWhitespace);

        var envelope = new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", Soap.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wsse", Wsse.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wsu", Wsu.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "for", Foriba.NamespaceName),
            BuildHeader(username, password, utcNow),
            new XElement(Soap + "Body",
                new XElement(Foriba + "SubmitInvoiceRequest",
                    new XElement(Foriba + "Action", action),
                    new XElement(Foriba + "DocumentUuid", documentUuid),
                    new XElement(Foriba + "Invoice", ublElement))));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), envelope).ToString(SaveOptions.DisableFormatting);
    }

    public static string BuildCancelRequest(string username, string password, string uuid, string reason, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uuid);

        var envelope = new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", Soap.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wsse", Wsse.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wsu", Wsu.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "for", Foriba.NamespaceName),
            BuildHeader(username, password, utcNow),
            new XElement(Soap + "Body",
                new XElement(Foriba + "CancelInvoiceRequest",
                    new XElement(Foriba + "Uuid", uuid),
                    new XElement(Foriba + "Reason", reason ?? string.Empty))));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), envelope).ToString(SaveOptions.DisableFormatting);
    }

    public static string BuildCreditNoteRequest(
        string username,
        string password,
        string originalUuid,
        string ublXmlBody,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalUuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(ublXmlBody);

        var ublElement = XElement.Parse(ublXmlBody, LoadOptions.PreserveWhitespace);

        var envelope = new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", Soap.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wsse", Wsse.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wsu", Wsu.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "for", Foriba.NamespaceName),
            BuildHeader(username, password, utcNow),
            new XElement(Soap + "Body",
                new XElement(Foriba + "CreditNoteRequest",
                    new XElement(Foriba + "OriginalUuid", originalUuid),
                    new XElement(Foriba + "CreditNote", ublElement))));

        return new XDocument(new XDeclaration("1.0", "utf-8", null), envelope).ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildHeader(string username, string password, DateTime utcNow)
    {
        var createdValue = utcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var tokenId = "UsernameToken-" + Guid.NewGuid().ToString("N");

        return new XElement(Soap + "Header",
            new XElement(Wsse + "Security",
                new XAttribute(Soap + "mustUnderstand", "1"),
                new XElement(Wsse + "UsernameToken",
                    new XAttribute(Wsu + "Id", tokenId),
                    new XElement(Wsse + "Username", username),
                    new XElement(Wsse + "Password",
                        new XAttribute("Type", PasswordTextType),
                        password),
                    new XElement(Wsse + "Nonce",
                        new XAttribute("EncodingType",
                            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"),
                        Convert.ToBase64String(Guid.NewGuid().ToByteArray())),
                    new XElement(Wsu + "Created", createdValue))));
    }
}
