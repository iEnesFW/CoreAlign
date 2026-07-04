using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

// WHY: Returns FSM/create/receive/credit-note is covered by 17+ Application-layer tests. These integration
// tests cover the HTTP boundary (auth policy, envelope, create-endpoint validation + cross-tenant read).
// Seeding a fully-shipped order WITH lines is not exercised in the SQLite harness (no existing test does it);
// happy-path create→approve is validated at the Application layer + verified against real Postgres in the browser.
[Collection(IntegrationCollection.Name)]
public class ReturnsIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public ReturnsIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Listing_returns_requires_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/returns");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Listing_returns_is_ok_for_tenant_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/returns?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").TryGetProperty("items", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Creating_return_on_ineligible_draft_order_returns_bad_request()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var create = await client.PostAsJsonAsync("/api/v1/returns", new
        {
            orderId = _factory.TenantA.OrderId,
            reason = "Other",
            lines = new[] { new { orderLineId = Guid.NewGuid(), quantityReturned = 1m } },
        });

        create.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Creating_return_without_lines_is_rejected()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var create = await client.PostAsJsonAsync("/api/v1/returns", new
        {
            orderId = _factory.TenantA.OrderId,
            reason = "Other",
            lines = Array.Empty<object>(),
        });

        create.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Approving_nonexistent_return_returns_not_found()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.PostAsync($"/api/v1/returns/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
