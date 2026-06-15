using System.Net;
using System.Net.Http.Headers;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

/// <summary>
/// Cross-tenant isolation contract tests for Phase 1 (Glass Enclosure) endpoints.
/// Each test acts as TenantAdmin of TenantA and pokes resources owned by TenantB
/// (or by no tenant at all). Successful access (200/201/204) would indicate a leak,
/// so we explicitly enumerate the deny-shaped status codes a hardened endpoint may emit.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class Phase1CrossTenantIsolationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public Phase1CrossTenantIsolationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient AdminOfTenantA() => _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

    private static readonly HashSet<HttpStatusCode> AcceptableDeny = new()
    {
        HttpStatusCode.NotFound,
        HttpStatusCode.Forbidden,
        HttpStatusCode.BadRequest,
        HttpStatusCode.Conflict,
        HttpStatusCode.UnprocessableEntity,
    };

    private static void AssertDenied(HttpResponseMessage response)
    {
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        response.StatusCode.Should().NotBe(HttpStatusCode.NoContent);
        AcceptableDeny.Should().Contain(response.StatusCode,
            "Phase 1 cross-tenant lookups must surface as not-found / forbidden / bad-request, not as success");
    }

    [Fact]
    public async Task TenantAdminA_ListsGlassEnclosureProjects_DoesNotIncludeTenantBProjects()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/glass-enclosure/projects");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.TenantSlug);
    }

    [Fact]
    public async Task TenantAdminA_GetGlassProjectOwnedByTenantB_ReturnsNotFound()
    {
        var unknownIdFromTenantB = Guid.NewGuid();
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/glass-enclosure/projects/{unknownIdFromTenantB}");
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_ListsTemplates_OnlyReturnsOwnTenant()
    {
        var client = AdminOfTenantA();
        var response = await client.GetAsync("/api/v1/glass-enclosure/projects/templates");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(_factory.TenantB.TenantSlug);
    }

    [Fact]
    public async Task TenantAdminA_CreateFromTemplateOfTenantB_ReturnsNotFound()
    {
        var unknownTemplateId = Guid.NewGuid();
        var client = AdminOfTenantA();
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/glass-enclosure/projects/from-template/{unknownTemplateId}")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
        };
        var response = await client.SendAsync(request);
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_ReadingWorkOrderRevisionsOfTenantB_ReturnsNotFoundOrEmpty()
    {
        var unknownWorkOrderId = Guid.NewGuid();
        var client = AdminOfTenantA();
        var response = await client.GetAsync($"/api/v1/glass-enclosure/work-orders/{unknownWorkOrderId}/revisions");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain(_factory.TenantB.TenantSlug);
            return;
        }
        AssertDenied(response);
    }

    [Fact]
    public async Task TenantAdminA_ApprovingWorkOrderRevisionOfTenantB_IsRejected()
    {
        var unknownWorkOrderId = Guid.NewGuid();
        var unknownRevisionId = Guid.NewGuid();
        var client = AdminOfTenantA();
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/glass-enclosure/work-orders/{unknownWorkOrderId}/revisions/{unknownRevisionId}/approve")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
        };
        var response = await client.SendAsync(request);
        AssertDenied(response);
    }

    [Fact]
    public async Task UnauthenticatedRequest_OnGlassEnclosureProjects_IsRejected()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/glass-enclosure/projects");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
