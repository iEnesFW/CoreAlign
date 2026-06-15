using System.Net;
using System.Net.Http.Headers;

namespace CoreAlign.Integration.Tests.Infrastructure;

[Collection(IntegrationCollection.Name)]
public class EtagMiddlewareTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public EtagMiddlewareTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AdminOfTenantA() =>
        _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

    [Fact]
    public async Task GET_returns_etag_header_on_successful_json_response()
    {
        var client = AdminOfTenantA();

        var response = await client.GetAsync($"/api/v1/Customers/{_factory.TenantA.CustomerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.Tag.Should().StartWith("\"").And.EndWith("\"");
    }

    [Fact]
    public async Task GET_with_matching_if_none_match_returns_304()
    {
        var client = AdminOfTenantA();
        var first = await client.GetAsync($"/api/v1/Customers/{_factory.TenantA.CustomerId}");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.Should().NotBeNull();

        using var conditional = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/Customers/{_factory.TenantA.CustomerId}");
        conditional.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag!.Tag));
        var second = await client.SendAsync(conditional);

        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task GET_with_different_if_none_match_returns_200()
    {
        var client = AdminOfTenantA();

        using var conditional = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/Customers/{_factory.TenantA.CustomerId}");
        conditional.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef\""));
        var response = await client.SendAsync(conditional);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
    }
}
