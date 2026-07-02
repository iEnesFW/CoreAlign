using System.Net;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class CustomersExportIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public CustomersExportIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Exporting_customers_requires_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/customers/export?format=Xlsx");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Exporting_customers_as_xlsx_returns_a_spreadsheet_file()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/customers/export?format=Xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        (await response.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Exporting_customers_as_csv_returns_a_csv_file()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/customers/export?format=Csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }
}
