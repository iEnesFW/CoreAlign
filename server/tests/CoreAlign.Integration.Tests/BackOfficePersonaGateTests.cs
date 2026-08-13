using System.Net;
using System.Net.Http.Json;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

// WHY this exists: a portal contact is a real User row inside the tenant, so their JWT carries
// tenant_id and a bare [Authorize] admitted them to every back-office controller. The handlers
// only scope by tenant, never by ownership — so a customer could list every invoice in the tenant
// and credit their own debt away. The persona claim is the only thing separating them from staff.
[Collection(IntegrationCollection.Name)]
public class BackOfficePersonaGateTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public BackOfficePersonaGateTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AsCustomer() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

    private HttpClient AsDealer() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Dealer);

    private HttpClient AsTenantAdmin() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

    public static TheoryData<string> BackOfficeReads() =>
        new()
        {
            "/api/v1/invoices",
            "/api/v1/payments",
            "/api/v1/orders",
            "/api/v1/customers",
            "/api/v1/products",
            "/api/v1/vendors",
            "/api/v1/purchase-orders",
            "/api/v1/stock/items",
        };

    [Theory]
    [MemberData(nameof(BackOfficeReads))]
    public async Task Customer_persona_cannot_read_back_office_endpoints(string path)
    {
        var response = await AsCustomer().GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(BackOfficeReads))]
    public async Task Dealer_persona_cannot_read_back_office_endpoints(string path)
    {
        var response = await AsDealer().GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_persona_cannot_issue_a_credit_note()
    {
        var response = await AsCustomer().PostAsJsonAsync(
            $"/api/v1/invoices/{Guid.NewGuid()}/credit-notes",
            new { lines = Array.Empty<object>(), reason = "self service refund" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Customer_persona_cannot_mark_an_invoice_paid()
    {
        var response = await AsCustomer().PostAsJsonAsync(
            $"/api/v1/invoices/{Guid.NewGuid()}/mark-paid",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tenant_staff_still_reach_the_back_office_reads()
    {
        var response = await AsTenantAdmin().GetAsync("/api/v1/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
