using System.Net;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class NPlusOneRegressionTests
{
    private const int CustomersListMaxRoundTrips = 6;
    private const int ProductsListMaxRoundTrips = 6;
    private const int OrdersListMaxRoundTrips = 8;

    private readonly CoreAlignWebApiFactory _factory;

    public NPlusOneRegressionTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AdminOfTenantA() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

    [Fact]
    public async Task CustomersListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client);

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/Customers?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            CustomersListMaxRoundTrips,
            $"GET /api/v1/Customers MUST stay within {CustomersListMaxRoundTrips} round trips to avoid N+1 (observed {counter.Total})");
    }

    [Fact]
    public async Task ProductsListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client);

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/Products?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            ProductsListMaxRoundTrips,
            $"GET /api/v1/Products MUST stay within {ProductsListMaxRoundTrips} round trips to avoid N+1 (observed {counter.Total})");
    }

    [Fact]
    public async Task OrdersListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client);

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/Orders?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            OrdersListMaxRoundTrips,
            $"GET /api/v1/Orders MUST stay within {OrdersListMaxRoundTrips} round trips to avoid N+1 (observed {counter.Total})");
    }

    private static async Task WarmUpAsync(HttpClient client)
    {
        var warmup = await client.GetAsync("/api/v1/health");
        warmup.Dispose();
    }
}
