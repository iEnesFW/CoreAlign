using System.Net;
using System.Net.Http.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

/// <summary>
/// The entry form asks "does anyone already carry this identity?" before the operator saves a
/// second record for the same company. Advisory only — it must never block, and it must never
/// reveal another tenant's customers.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class CustomerDuplicateCheckTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public CustomerDuplicateCheckTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(string TaxNumber, string Name)> SeedAsync(Guid tenantId, string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        var taxNumber = $"99{suffix}";
        var customer = new Customer($"Duplicate Probe {suffix}", taxNumber: taxNumber) { TenantId = tenantId };
        db.Set<Customer>().Add(customer);
        await db.SaveChangesAsync();
        return (taxNumber, customer.Name);
    }

    [Fact]
    public async Task An_existing_tax_number_is_reported_back_to_the_form()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (taxNumber, name) = await SeedAsync(_factory.TenantA.TenantId, suffix);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync($"/api/v1/customers/duplicate-check?taxNumber={taxNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(name);
    }

    [Fact]
    public async Task Another_tenants_customer_is_never_reported()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var (taxNumber, name) = await SeedAsync(_factory.TenantB.TenantId, suffix);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync($"/api/v1/customers/duplicate-check?taxNumber={taxNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(name);
    }

    [Fact]
    public async Task A_query_with_no_identity_returns_nothing_rather_than_the_whole_book()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/customers/duplicate-check");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope>();
        payload!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task The_endpoint_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/customers/duplicate-check?taxNumber=123");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ApiEnvelope(List<DuplicateRow> Data);

    private sealed record DuplicateRow(Guid Id, string Name);
}
