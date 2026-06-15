using System.Net;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class MyInvoicesControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    static MyInvoicesControllerIntegrationTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public MyInvoicesControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_caller_receives_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/customer-portal/invoices/{Guid.NewGuid()}/download-pdf");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DownloadPdf_returns_pdf_for_own_invoice()
    {
        await EnsureIssuedInvoiceWithLineAsync(_factory.TenantA, _factory.TenantA.InvoiceId);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync($"/api/v1/customer-portal/invoices/{_factory.TenantA.InvoiceId}/download-pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DownloadPdf_for_cross_tenant_invoice_returns_not_found()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync($"/api/v1/customer-portal/invoices/{_factory.TenantB.InvoiceId}/download-pdf");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadPdf_for_missing_invoice_returns_not_found()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync($"/api/v1/customer-portal/invoices/{Guid.NewGuid()}/download-pdf");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Dealer_persona_cannot_download_customer_portal_invoice_pdf()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Dealer);

        var response = await client.GetAsync($"/api/v1/customer-portal/invoices/{_factory.TenantA.InvoiceId}/download-pdf");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private async Task EnsureIssuedInvoiceWithLineAsync(TenantFixture tenant, Guid invoiceId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            await db.Invoices
                .Where(i => i.Id == invoiceId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.Status, Domain.Enums.InvoiceStatus.Issued));
        }
    }
}
