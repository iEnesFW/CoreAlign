using System.Xml.Linq;
using CoreAlign.Application.EInvoice;
using CoreAlign.Domain.Entities;

namespace CoreAlign.Application.Tests.EInvoice;

// e-İrsaliye (§1.3-D4): the DespatchAdvice-2 UBL-TR builder must emit a proper despatch document
// carrying carrier (VKN), driver (TCKN) and vehicle plate, and one DespatchLine per shipment line.
public class DespatchAdviceUblBuilderTests
{
    private static readonly SellerParty Seller =
        new("Satıcı A.Ş.", "9999999999", null, "Kadıköy", "Cadde 1", "İstanbul", "34000", "Türkiye");

    private static readonly BuyerParty Buyer =
        new("Alıcı Ltd.", "8888888888", null, "Beşiktaş", "Sokak 2", "İstanbul", "34100", "Türkiye");

    private static Shipment DispatchedShipment()
    {
        var shipment = new Shipment("SHP-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null) { Id = Guid.NewGuid() };
        shipment.AddLine(new ShipmentLine(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Widget", 5m, 10m));
        shipment.MarkPicked(null);
        shipment.MarkPacked();
        shipment.Dispatch("Aras Kargo", "TRK-1", null, 50m);
        shipment.SetEDespatchCarrier("1234567890", "34ABC123", "Ahmet Yılmaz", "12345678901");
        shipment.SetEDespatchProfile("TEMELIRSALIYE");
        return shipment;
    }

    [Fact]
    public void BuildDespatch_emits_despatch_advice_with_carrier_driver_plate_and_lines()
    {
        var xml = UblTrInvoiceXmlBuilder.BuildDespatch(DispatchedShipment(), Seller, Buyer);
        var doc = XDocument.Parse(xml);

        doc.Root!.Name.LocalName.Should().Be("DespatchAdvice");
        First(doc, "ProfileID").Should().Be("TEMELIRSALIYE");
        First(doc, "ID").Should().Be("SHP-1");
        First(doc, "DespatchAdviceTypeCode").Should().Be("SEVK");

        var carrier = doc.Descendants().First(e => e.Name.LocalName == "CarrierParty");
        carrier.Descendants().First(e => e.Name.LocalName == "ID").Value.Should().Be("1234567890");

        var driver = doc.Descendants().First(e => e.Name.LocalName == "DriverPerson");
        driver.Descendants().First(e => e.Name.LocalName == "NationalID").Value.Should().Be("12345678901");

        First(doc, "LicensePlateID").Should().Be("34ABC123");

        var lines = doc.Descendants().Where(e => e.Name.LocalName == "DespatchLine").ToList();
        lines.Should().HaveCount(1);
        lines[0].Descendants().First(e => e.Name.LocalName == "DeliveredQuantity").Value.Should().Be("5.00");
    }

    [Fact]
    public void BuildDespatch_delivery_customer_party_carries_buyer_vkn()
    {
        var xml = UblTrInvoiceXmlBuilder.BuildDespatch(DispatchedShipment(), Seller, Buyer);
        var doc = XDocument.Parse(xml);

        var deliveryParty = doc.Descendants().First(e => e.Name.LocalName == "DeliveryCustomerParty");
        deliveryParty.Descendants().First(e => e.Name.LocalName == "ID").Value.Should().Be("8888888888");
    }

    private static string First(XDocument doc, string localName) =>
        doc.Descendants().First(e => e.Name.LocalName == localName).Value;
}
