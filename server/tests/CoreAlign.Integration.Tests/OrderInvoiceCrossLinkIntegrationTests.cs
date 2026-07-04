using System.Net;
using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class OrderInvoiceCrossLinkIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public OrderInvoiceCrossLinkIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Orders_list_shows_active_invoice_and_shipment_numbers_and_clears_them_after_cancellation()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        Guid orderId;
        Guid invoiceId;
        Guid shipmentId;
        var invoiceNumber = $"INV-XL-{suffix}";
        var shipmentNumber = $"SHP-XL-{suffix}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

            var order = new Order(
                orderNumber: $"ORD-XL-{suffix}",
                customerId: _factory.TenantA.CustomerId,
                orderDate: DateTime.UtcNow,
                currency: "TRY",
                notes: null);
            order.TenantId = _factory.TenantA.TenantId;
            db.Orders.Add(order);

            var invoice = new Invoice(invoiceNumber, _factory.TenantA.CustomerId, "XL Customer", "TRY");
            invoice.AttachToOrder(order.Id);
            invoice.TenantId = _factory.TenantA.TenantId;
            db.Invoices.Add(invoice);

            var warehouse = new Warehouse($"WH-XL-{suffix}", $"Warehouse XL {suffix}");
            warehouse.TenantId = _factory.TenantA.TenantId;
            db.Warehouses.Add(warehouse);

            var shipment = new Shipment(shipmentNumber, order.Id, _factory.TenantA.CustomerId, warehouse.Id, null);
            shipment.TenantId = _factory.TenantA.TenantId;
            db.Shipments.Add(shipment);

            await db.SaveChangesAsync();
            orderId = order.Id;
            invoiceId = invoice.Id;
            shipmentId = shipment.Id;
        }

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var row = await GetOrderRowAsync(client, orderId);
        row.GetProperty("invoiceId").GetGuid().Should().Be(invoiceId);
        row.GetProperty("invoiceNumber").GetString().Should().Be(invoiceNumber);
        row.GetProperty("shipmentId").GetGuid().Should().Be(shipmentId);
        row.GetProperty("shipmentNumber").GetString().Should().Be(shipmentNumber);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            var invoice = await db.Invoices.IgnoreQueryFilters().SingleAsync(i => i.Id == invoiceId);
            invoice.Cancel(DateTime.UtcNow);
            var shipment = await db.Shipments.IgnoreQueryFilters().SingleAsync(s => s.Id == shipmentId);
            shipment.Cancel("cross-link test");
            await db.SaveChangesAsync();
        }

        var rowAfter = await GetOrderRowAsync(client, orderId);
        AssertNullOrMissing(rowAfter, "invoiceId");
        AssertNullOrMissing(rowAfter, "invoiceNumber");
        AssertNullOrMissing(rowAfter, "shipmentId");
        AssertNullOrMissing(rowAfter, "shipmentNumber");
    }

    private static void AssertNullOrMissing(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            value.ValueKind.Should().Be(
                JsonValueKind.Null,
                $"'{propertyName}' must be cleared once the linked document is cancelled");
        }
    }

    [Fact]
    public async Task Invoices_list_shows_linked_order_number()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var orderNumber = $"ORD-XR-{suffix}";
        Guid invoiceId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

            var order = new Order(
                orderNumber: orderNumber,
                customerId: _factory.TenantA.CustomerId,
                orderDate: DateTime.UtcNow,
                currency: "TRY",
                notes: null);
            order.TenantId = _factory.TenantA.TenantId;
            db.Orders.Add(order);

            var invoice = new Invoice($"INV-XR-{suffix}", _factory.TenantA.CustomerId, "XR Customer", "TRY");
            invoice.AttachToOrder(order.Id);
            invoice.TenantId = _factory.TenantA.TenantId;
            db.Invoices.Add(invoice);

            await db.SaveChangesAsync();
            invoiceId = invoice.Id;
        }

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync("/api/v1/Invoices?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var row = FindRowById(await response.Content.ReadAsStringAsync(), invoiceId);
        row.GetProperty("orderNumber").GetString().Should().Be(orderNumber);
    }

    private static async Task<JsonElement> GetOrderRowAsync(HttpClient client, Guid orderId)
    {
        var response = await client.GetAsync("/api/v1/Orders?page=1&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return FindRowById(await response.Content.ReadAsStringAsync(), orderId);
    }

    private static JsonElement FindRowById(string json, Guid id)
    {
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("data").GetProperty("items");
        foreach (var el in items.EnumerateArray())
        {
            if (el.GetProperty("id").GetGuid() == id)
            {
                return el.Clone();
            }
        }

        throw new InvalidOperationException($"Row {id} not found in list response.");
    }
}
