using System.Net;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class DocumentNumberGapEndpointTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public DocumentNumberGapEndpointTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DocumentNumberGaps_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/document-number-gaps");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DocumentNumberGaps_returns_report_for_authenticated_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync("/api/v1/reports/document-number-gaps?year=2026");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("totalGap");
    }
}
