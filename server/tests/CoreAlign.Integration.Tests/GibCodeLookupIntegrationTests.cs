using System.Net;
using System.Text.Json;
using CoreAlign.API.HostedServices;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class GibCodeLookupIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public GibCodeLookupIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureSeededAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await GibCodeSeed.SeedGlobalAsync(scope.ServiceProvider, CancellationToken.None);
    }

    [Fact]
    public async Task Withholding_codes_endpoint_returns_seeded_gib_list()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/master-data/withholding-tax-codes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();
        items.Count.Should().BeGreaterThanOrEqualTo(52);

        var yapim = items.Single(x => x.GetProperty("code").GetString() == "601");
        yapim.GetProperty("numerator").GetInt32().Should().Be(4);
        yapim.GetProperty("denominator").GetInt32().Should().Be(10);
        yapim.GetProperty("kind").GetString().Should().Be("Partial");

        var tamTevkifat = items.Single(x => x.GetProperty("code").GetString() == "801");
        tamTevkifat.GetProperty("numerator").GetInt32().Should().Be(10);
        tamTevkifat.GetProperty("kind").GetString().Should().Be("Full");
    }

    [Fact]
    public async Task Exemption_codes_endpoint_returns_seeded_gib_list()
    {
        await EnsureSeededAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/master-data/vat-exemption-codes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.GetProperty("data").EnumerateArray().ToList();
        items.Count.Should().BeGreaterThanOrEqualTo(85);

        var malIhracati = items.Single(x => x.GetProperty("code").GetString() == "301");
        malIhracati.GetProperty("kind").GetString().Should().Be("Full");
        malIhracati.GetProperty("lawReference").GetString().Should().Be("KDVK 11/1-a");

        items.Should().Contain(x => x.GetProperty("code").GetString() == "250");
        items.Should().Contain(x => x.GetProperty("code").GetString() == "701");
    }

    [Fact]
    public async Task Codes_are_globally_visible_to_every_tenant()
    {
        await EnsureSeededAsync();
        var clientB = _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);

        var response = await clientB.GetAsync("/api/v1/master-data/withholding-tax-codes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("data").EnumerateArray()
            .Should().Contain(x => x.GetProperty("code").GetString() == "601");
    }

    [Fact]
    public async Task Lookup_endpoints_require_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/master-data/withholding-tax-codes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Seeding_twice_does_not_duplicate_codes()
    {
        await EnsureSeededAsync();
        await EnsureSeededAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/master-data/withholding-tax-codes");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var codes = doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => x.GetProperty("code").GetString())
            .ToList();
        codes.Should().OnlyHaveUniqueItems();
    }
}
