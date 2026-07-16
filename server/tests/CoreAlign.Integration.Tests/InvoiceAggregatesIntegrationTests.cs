using System.Net;
using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class InvoiceAggregatesIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public InvoiceAggregatesIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Aggregates_sum_the_whole_tenant_result_set_not_just_one_page()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

            // open + due-soon (unpaid, due in 3 days)
            db.Invoices.Add(BuildInvoice($"INV-AGG-{suffix}-1", _factory.TenantA.TenantId, _factory.TenantA.CustomerId, 1000m, now.AddDays(3), "issued"));
            // open + partially paid (200 of 500, due far out)
            db.Invoices.Add(BuildInvoice($"INV-AGG-{suffix}-2", _factory.TenantA.TenantId, _factory.TenantA.CustomerId, 500m, now.AddDays(30), "partial", 200m));
            // overdue (unpaid, due 5 days ago)
            db.Invoices.Add(BuildInvoice($"INV-AGG-{suffix}-3", _factory.TenantA.TenantId, _factory.TenantA.CustomerId, 800m, now.AddDays(-5), "issued"));
            // paid
            db.Invoices.Add(BuildInvoice($"INV-AGG-{suffix}-4", _factory.TenantA.TenantId, _factory.TenantA.CustomerId, 400m, now.AddDays(10), "paid"));
            // cancelled
            db.Invoices.Add(BuildInvoice($"INV-AGG-{suffix}-5", _factory.TenantA.TenantId, _factory.TenantA.CustomerId, 600m, now.AddDays(10), "cancelled"));

            // Cross-tenant noise sharing the same search prefix — must be excluded from Tenant-A's totals.
            db.Invoices.Add(BuildInvoice($"INV-AGG-{suffix}-B", _factory.TenantB.TenantId, _factory.TenantB.CustomerId, 9999m, now.AddDays(3), "issued"));

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync($"/api/v1/Invoices/aggregates?search=AGG-{suffix}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");

        data.GetProperty("totalCount").GetInt32().Should().Be(5, "the Tenant-B row is excluded by the tenant filter");
        data.GetProperty("openCount").GetInt32().Should().Be(2);
        data.GetProperty("partiallyPaidCount").GetInt32().Should().Be(1);
        data.GetProperty("overdueCount").GetInt32().Should().Be(1);
        data.GetProperty("paidCount").GetInt32().Should().Be(1);
        data.GetProperty("cancelledCount").GetInt32().Should().Be(1);
        data.GetProperty("dueSoonCount").GetInt32().Should().Be(1);
        data.GetProperty("outstandingTotal").GetDecimal().Should().Be(2700m);
        data.GetProperty("paidTotal").GetDecimal().Should().Be(600m);
        data.GetProperty("overdueTotal").GetDecimal().Should().Be(800m);
    }

    [Fact]
    public async Task Aggregates_endpoint_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/Invoices/aggregates");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Invoice BuildInvoice(
        string number,
        Guid tenantId,
        Guid customerId,
        decimal total,
        DateTime dueDate,
        string state,
        decimal partialPaid = 0m)
    {
        var invoice = new Invoice(number, customerId, "Aggregate Customer", "TRY");
        invoice.UpdateDetails(
            issueDate: DateTime.UtcNow,
            dueDate: dueDate,
            postingDate: DateTime.UtcNow,
            exchangeRate: 1m,
            paymentTermsId: null,
            paymentTermsNetDaysSnapshot: null,
            headerDiscountPercent: 0m,
            headerDiscountAmount: 0m,
            shippingCost: 0m,
            roundingAdjustment: 0m,
            internalNotes: null,
            publicNotes: null,
            termsAndConditions: null,
            notes: null);
        invoice.ReplaceLines(new[] { new InvoiceLine("SKU-AGG", "Aggregate line", null, 1m, total) });
        invoice.TenantId = tenantId;
        // Child lines are TenantEntities too; the raw DbContext scope has no HTTP tenant
        // context to auto-stamp them, so set the FK explicitly.
        foreach (var line in invoice.Lines)
        {
            line.TenantId = tenantId;
        }

        if (state != "draft")
        {
            invoice.Issue(number);
        }

        switch (state)
        {
            case "paid":
                invoice.MarkAsPaid(DateTime.UtcNow);
                break;
            case "partial":
                invoice.RecordPayment(partialPaid, DateTime.UtcNow);
                break;
            case "cancelled":
                invoice.Cancel(DateTime.UtcNow);
                break;
        }

        // Read-model fixture: drop the lifecycle events so no ledger/e-invoice handlers fire on save.
        invoice.ClearDomainEvents();
        return invoice;
    }
}
