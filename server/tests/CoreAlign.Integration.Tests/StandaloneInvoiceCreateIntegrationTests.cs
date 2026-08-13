using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

/// <summary>
/// The standalone-invoice screen now sends a header discount, a shipping cost and per-line
/// discounts. Those fields existed on the command long before any screen filled them in, so this
/// locks the wire contract: what the form previews must be what the server books.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class StandaloneInvoiceCreateIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public StandaloneInvoiceCreateIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private object BuildPayload(Guid customerId) => new
    {
        customerId,
        issueDate = DateTime.UtcNow.Date,
        currency = "TRY",
        dueDays = 30,
        headerDiscountPercent = 5m,
        shippingCost = 25m,
        lines = new[]
        {
            new
            {
                productId = (Guid?)null,
                productSku = "SKU-M4",
                productName = "M4 line",
                description = (string?)null,
                quantity = 2m,
                unitPrice = 100m,
                taxRatePercent = 20m,
                lineDiscountPercent = 10m,
            },
        },
    };

    [Fact]
    public async Task Header_discount_shipping_and_line_discount_all_reach_the_booked_invoice()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.PostAsJsonAsync(
            "/api/v1/Invoices/standalone",
            BuildPayload(_factory.TenantA.CustomerId));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");

        // 2 × 100 = 200 gross, −10% line = 180, −5% header = 171 taxable, VAT 20% of 180 = 36,
        // plus 25 shipping → 232. This is exactly what the form's summary panel previews.
        // The fixture tenant has no seeded document sequences, so this also proves a first-ever
        // invoice numbers itself instead of failing with "sequence is not seeded".
        data.GetProperty("invoiceNumber").GetString().Should().StartWith("INV-");

        data.GetProperty("subtotal").GetDecimal().Should().Be(200m);
        data.GetProperty("taxableTotal").GetDecimal().Should().Be(171m);
        data.GetProperty("taxTotal").GetDecimal().Should().Be(36m);
        data.GetProperty("shippingCost").GetDecimal().Should().Be(25m);
        data.GetProperty("headerDiscountPercent").GetDecimal().Should().Be(5m);
        data.GetProperty("total").GetDecimal().Should().Be(232m);

        var line = data.GetProperty("lines").EnumerateArray().Single();
        line.GetProperty("lineDiscountPercent").GetDecimal().Should().Be(10m);
        line.GetProperty("lineTotal").GetDecimal().Should().Be(216m, "the line total is VAT-inclusive: 180 net + 36 VAT");
    }

    [Fact]
    public async Task Creating_a_standalone_invoice_requires_authentication()
    {
        var response = await _factory.CreateClient()
            .PostAsJsonAsync("/api/v1/Invoices/standalone", BuildPayload(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Another_tenants_customer_cannot_be_invoiced()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.PostAsJsonAsync(
            "/api/v1/Invoices/standalone",
            BuildPayload(_factory.TenantB.CustomerId));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }
}
