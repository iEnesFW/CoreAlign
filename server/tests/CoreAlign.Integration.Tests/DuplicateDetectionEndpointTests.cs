using System.Net;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class DuplicateDetectionEndpointTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public DuplicateDetectionEndpointTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Duplicates_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/reports/duplicates?entity=customer&key=Email");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("customer", "Email")]
    [InlineData("customer", "TaxNumber")]
    [InlineData("vendor", "Email")]
    [InlineData("vendor", "NationalId")]
    public async Task Duplicates_two_pass_groupby_executes_for_authenticated_admin(string entity, string key)
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync($"/api/v1/reports/duplicates?entity={entity}&key={key}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("groupCount");
    }
}
