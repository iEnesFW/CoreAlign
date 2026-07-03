using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class CustomerNotesIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public CustomerNotesIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Adding_note_persists_and_is_listed_for_recipient_customer()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var body = $"Not-{Guid.NewGuid():N}";

        var post = await client.PostAsJsonAsync(
            $"/api/v1/customers/{_factory.TenantA.CustomerId}/notes",
            new { body });
        post.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await client.GetAsync($"/api/v1/customers/{_factory.TenantA.CustomerId}/notes");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await list.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var bodies = doc.RootElement.GetProperty("data").EnumerateArray()
            .Select(el => el.GetProperty("body").GetString())
            .ToList();
        bodies.Should().Contain(body);
    }

    [Fact]
    public async Task Adding_note_to_another_tenants_customer_is_denied()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{_factory.TenantB.CustomerId}/notes",
            new { body = "cross-tenant" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Listing_another_tenants_customer_notes_is_denied()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync($"/api/v1/customers/{_factory.TenantB.CustomerId}/notes");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Adding_note_requires_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{_factory.TenantA.CustomerId}/notes",
            new { body = "anon" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
