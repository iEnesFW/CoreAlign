using System.Net;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class NPlusOneRegressionTests
{
    private const int CustomersListMaxRoundTrips = 3;
    private const int ProductsListMaxRoundTrips = 3;
    private const int OrdersListMaxRoundTrips = 3;
    private const int InvoicesListMaxRoundTrips = 3;
    private const int InvoiceDetailMaxRoundTrips = 3;
    private const int OrderDetailWithRevisionsMaxRoundTrips = 4;
    private const int QuotesListMaxRoundTrips = 3;
    private const int ReturnsListMaxRoundTrips = 3;
    private const int StockItemsListMaxRoundTrips = 3;
    private const int VendorBillsListMaxRoundTrips = 3;
    private const int VendorPaymentsListMaxRoundTrips = 3;
    private const int CustomerPortalInvoicesListMaxRoundTrips = 3;
    private const int CustomerPortalOrdersListMaxRoundTrips = 3;
    private const int DealerPortalInvoicesListMaxRoundTrips = 4;
    private const int DealerPortalOrdersListMaxRoundTrips = 4;
    private const int DealerPortalCommissionsListMaxRoundTrips = 4;

    private readonly CoreAlignWebApiFactory _factory;

    public NPlusOneRegressionTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AdminOfTenantA() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

    private HttpClient CustomerOfTenantA() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

    private HttpClient DealerOfTenantA() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Dealer);

    [Fact]
    public async Task CustomersListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client, "/api/v1/Customers?page=1&pageSize=25");

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
        await WarmUpAsync(client, "/api/v1/Products?page=1&pageSize=25");

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
        await WarmUpAsync(client, "/api/v1/Orders?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/Orders?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            OrdersListMaxRoundTrips,
            $"GET /api/v1/Orders MUST stay within {OrdersListMaxRoundTrips} round trips to avoid N+1 (observed {counter.Total})");
    }

    [Fact]
    public async Task InvoicesListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client, "/api/v1/Invoices?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/Invoices?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            InvoicesListMaxRoundTrips,
            $"GET /api/v1/Invoices MUST stay within {InvoicesListMaxRoundTrips} round trips to avoid N+1 (observed {counter.Total})");
    }

    [Fact]
    public async Task InvoiceDetailEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        var path = $"/api/v1/Invoices/{_factory.TenantA.InvoiceId}";
        await WarmUpAsync(client, path);

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            InvoiceDetailMaxRoundTrips,
            $"GET /api/v1/Invoices/{{id}} MUST stay within {InvoiceDetailMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task OrderDetailEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        var detailPath = $"/api/v1/Orders/{_factory.TenantA.OrderId}";
        var revisionsPath = $"/api/v1/Orders/{_factory.TenantA.OrderId}/revisions";
        await WarmUpAsync(client, detailPath);
        await WarmUpAsync(client, revisionsPath);

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var detail = await client.GetAsync(detailPath);
        var revisions = await client.GetAsync(revisionsPath);

        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        revisions.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        counter.Total.Should().BeLessThanOrEqualTo(
            OrderDetailWithRevisionsMaxRoundTrips,
            $"GET /api/v1/Orders/{{id}} + revisions tab MUST stay within {OrderDetailWithRevisionsMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task QuotesListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client, "/api/v1/Quotes?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/Quotes?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            QuotesListMaxRoundTrips,
            $"GET /api/v1/Quotes MUST stay within {QuotesListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task ReturnsListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client, "/api/v1/Returns?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/Returns?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            ReturnsListMaxRoundTrips,
            $"GET /api/v1/Returns MUST stay within {ReturnsListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task StockItemsListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client, "/api/v1/stock/items?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/stock/items?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            StockItemsListMaxRoundTrips,
            $"GET /api/v1/stock/items MUST stay within {StockItemsListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task VendorBillsListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client, "/api/v1/vendor-bills?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/vendor-bills?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            VendorBillsListMaxRoundTrips,
            $"GET /api/v1/vendor-bills MUST stay within {VendorBillsListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task VendorPaymentsListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = AdminOfTenantA();
        await WarmUpAsync(client, "/api/v1/vendor-payments?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/vendor-payments?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            VendorPaymentsListMaxRoundTrips,
            $"GET /api/v1/vendor-payments MUST stay within {VendorPaymentsListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact(Skip = "ERP-ROUTE-001: ambiguous route between MyInvoicesController & CustomerPortalController.GetInvoices — re-enable once one is removed (see docs/sprint11-blockers.md). Test logic is correct.")]
    public async Task CustomerPortalInvoicesListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = CustomerOfTenantA();
        await WarmUpAsync(client, "/api/v1/customer-portal/invoices?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/customer-portal/invoices?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            CustomerPortalInvoicesListMaxRoundTrips,
            $"GET /api/v1/customer-portal/invoices MUST stay within {CustomerPortalInvoicesListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task CustomerPortalOrdersListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = CustomerOfTenantA();
        await WarmUpAsync(client, "/api/v1/customer-portal/orders?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/customer-portal/orders?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            CustomerPortalOrdersListMaxRoundTrips,
            $"GET /api/v1/customer-portal/orders MUST stay within {CustomerPortalOrdersListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task DealerPortalInvoicesListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = DealerOfTenantA();
        await WarmUpAsync(client, "/api/v1/dealer-portal/invoices?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/dealer-portal/invoices?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            DealerPortalInvoicesListMaxRoundTrips,
            $"GET /api/v1/dealer-portal/invoices MUST stay within {DealerPortalInvoicesListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task DealerPortalOrdersListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = DealerOfTenantA();
        await WarmUpAsync(client, "/api/v1/dealer-portal/orders?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/dealer-portal/orders?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            DealerPortalOrdersListMaxRoundTrips,
            $"GET /api/v1/dealer-portal/orders MUST stay within {DealerPortalOrdersListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    [Fact]
    public async Task DealerPortalCommissionsListEndpoint_StaysWithinRoundTripBudget()
    {
        var client = DealerOfTenantA();
        await WarmUpAsync(client, "/api/v1/dealer-portal/commissions?page=1&pageSize=25");

        using var counter = DbCommandRoundTripInterceptor.BeginScope();
        var response = await client.GetAsync("/api/v1/dealer-portal/commissions?page=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        counter.Total.Should().BeLessThanOrEqualTo(
            DealerPortalCommissionsListMaxRoundTrips,
            $"GET /api/v1/dealer-portal/commissions MUST stay within {DealerPortalCommissionsListMaxRoundTrips} round trips (observed {counter.Total})");
    }

    private static async Task WarmUpAsync(HttpClient client, string path)
    {
        var warmup = await client.GetAsync(path);
        warmup.Dispose();
    }
}
