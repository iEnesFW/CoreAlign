using System.Net;
using System.Net.Http.Json;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests.Manufacturing;

[Collection(IntegrationCollection.Name)]
public class ProductionJobEndpointsTests
{
    private const string ListUrl = "/api/v1/production-jobs";

    private readonly CoreAlignWebApiFactory _factory;

    public ProductionJobEndpointsTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private static string DetailUrl(Guid id) => $"{ListUrl}/{id}";

    [Fact]
    public async Task Listing_jobs_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync(ListUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listing_jobs_succeeds_for_an_authenticated_tenant_user()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync(ListUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Creating_a_job_requires_authentication()
    {
        var response = await _factory
            .CreateClient()
            .PostAsJsonAsync(ListUrl, new { productId = Guid.NewGuid(), plannedQuantity = 5m, unitOfMeasure = "PCS" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_a_job_is_forbidden_for_non_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.PostAsJsonAsync(
            ListUrl,
            new { productId = Guid.NewGuid(), plannedQuantity = 5m, unitOfMeasure = "PCS" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Fetching_an_unknown_job_is_not_found()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync(DetailUrl(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
