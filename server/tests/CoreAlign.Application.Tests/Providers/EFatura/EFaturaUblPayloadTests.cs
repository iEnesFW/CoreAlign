using System.Text;
using CoreAlign.Application.Providers.EFatura;
using CoreAlign.Infrastructure.Providers.EFatura;

namespace CoreAlign.Application.Tests.Providers.EFatura;

// Guards the F8 fix: the dispatcher used to base64 a `<Invoice><DocumentNumber/></Invoice>` stub,
// so lines/tax/totals/carrier never reached the provider. It must now send the real UBL-TR
// verbatim when the upstream document carries it. (The Nilvera network integration test is
// skipped in CI; this is the payload-shape guard that actually runs.)
public class EFaturaUblPayloadTests
{
    private static EFaturaDocument Doc(string? rawUbl) => new(
        EFaturaDocumentType.Invoice, "INV-1", DateTime.UtcNow, "1234567890", "Buyer",
        Array.Empty<EFaturaLine>(), "TRY", 100m, RawUblTrXml: rawUbl);

    [Fact]
    public void Sends_the_real_ubl_verbatim_when_present()
    {
        const string ubl =
            "<Invoice><cbc:ID>INV-1</cbc:ID><cac:InvoiceLine><cbc:LineExtensionAmount>100</cbc:LineExtensionAmount>" +
            "</cac:InvoiceLine><cac:TaxTotal><cbc:TaxAmount>18</cbc:TaxAmount></cac:TaxTotal></Invoice>";

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(EFaturaUblPayload.ToBase64(Doc(ubl))));

        decoded.Should().Be(ubl);
        decoded.Should().Contain("InvoiceLine").And.Contain("TaxTotal");
    }

    [Fact]
    public void Falls_back_to_document_number_stub_only_when_no_raw_ubl()
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(EFaturaUblPayload.ToBase64(Doc(null))));

        decoded.Should().Contain("INV-1");
        decoded.Should().NotContain("InvoiceLine");
    }
}
