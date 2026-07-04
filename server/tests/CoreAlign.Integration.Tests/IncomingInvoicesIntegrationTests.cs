using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class IncomingInvoicesIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public IncomingInvoicesIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<Guid> SeedIncomingAsync(Guid tenantId, string ettn, string vkn)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        var invoice = new IncomingInvoice(ettn, vkn, "Tedarikçi A.Ş.", $"GIB-{ettn}", DateTime.UtcNow, "nilvera", "Delivered")
        {
            TenantId = tenantId,
        };
        db.Set<IncomingInvoice>().Add(invoice);
        await db.SaveChangesAsync();
        return invoice.Id;
    }

    [Fact]
    public async Task Listing_returns_seeded_incoming_invoices()
    {
        var ettn = $"ETTN-{Guid.NewGuid():N}"[..20];
        await SeedIncomingAsync(_factory.TenantA.TenantId, ettn, "1234567890");
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/incoming-invoices?page=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").GetProperty("items").EnumerateArray();
        items.Should().Contain(x => x.GetProperty("ettn").GetString() == ettn);
    }

    [Fact]
    public async Task Processing_creates_vendor_bill_and_marks_processed()
    {
        var ettn = $"PRC-{Guid.NewGuid():N}"[..20];
        var id = await SeedIncomingAsync(_factory.TenantA.TenantId, ettn, $"{Random.Shared.Next(1000000000, 2000000000)}");
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var process = await client.PostAsJsonAsync(
            $"/api/v1/incoming-invoices/{id}/process",
            new { subtotal = 1000m, taxAmount = 200m, vendorName = "Yeni Tedarikçi", currency = "TRY" });

        process.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await process.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("vendorBillId").GetGuid().Should().NotBeEmpty();

        var detail = await client.GetAsync($"/api/v1/incoming-invoices/{id}");
        using var detailDoc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        detailDoc.RootElement.GetProperty("data").GetProperty("status").GetString().Should().Be("Processed");
    }

    [Fact]
    public async Task Ignoring_marks_invoice_ignored()
    {
        var ettn = $"IGN-{Guid.NewGuid():N}"[..20];
        var id = await SeedIncomingAsync(_factory.TenantA.TenantId, ettn, "1234567890");
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var ignore = await client.PostAsJsonAsync(
            $"/api/v1/incoming-invoices/{id}/ignore",
            new { reason = "Yinelenen belge" });

        ignore.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await ignore.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").GetProperty("status").GetString().Should().Be("Ignored");
    }

    [Fact]
    public async Task Cross_tenant_incoming_invoice_is_not_visible()
    {
        var ettn = $"XT-{Guid.NewGuid():N}"[..20];
        var id = await SeedIncomingAsync(_factory.TenantB.TenantId, ettn, "1234567890");
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync($"/api/v1/incoming-invoices/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Listing_requires_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/incoming-invoices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
