using System.Net;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class CashPositionEndpointTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public CashPositionEndpointTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CashPosition_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/cash-position");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CashPosition_returns_tenant_scoped_totals_for_authenticated_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync("/api/v1/reports/cash-position");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("totalCash");
    }
}
