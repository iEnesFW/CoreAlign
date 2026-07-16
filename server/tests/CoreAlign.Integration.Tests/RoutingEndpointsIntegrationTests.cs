using System.Net;
using System.Net.Http.Json;
using CoreAlign.Application.Common;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class RoutingEndpointsIntegrationTests
{
    private const string RoutingsUrl = "/api/v1/production-routings";
    private const string WorkCentersUrl = "/api/v1/work-centers";

    private readonly CoreAlignWebApiFactory _factory;

    public RoutingEndpointsIntegrationTests(CoreAlignWebApiFactory factory) => _factory = factory;

    private sealed record RoutingProbe(Guid Id, string Code, string Name, string Status);
    private sealed record WorkCenterProbe(Guid Id, string Code, string Name, bool IsActive);

    private static object NewRouting(string code) => new
    {
        code,
        name = "Temper hattı",
        description = "kesim-rodaj-temper",
    };

    private HttpClient AdminA() => _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
    private HttpClient AdminB() => _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);

    private static string UniqueCode(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..16];

    [Fact]
    public async Task Listing_routings_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync(RoutingsUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_routing_requires_tenant_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PostAsJsonAsync(RoutingsUrl, NewRouting(UniqueCode("R")));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_read_routing_roundtrip_starts_draft()
    {
        var client = AdminA();
        var code = UniqueCode("R");

        var create = await client.PostAsJsonAsync(RoutingsUrl, NewRouting(code));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<ApiResponse<RoutingProbe>>())!.Data!;
        created.Code.Should().Be(code);
        created.Status.Should().Be("Draft");

        var get = await client.GetAsync($"{RoutingsUrl}/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = (await get.Content.ReadFromJsonAsync<ApiResponse<RoutingProbe>>())!.Data!;
        fetched.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Duplicate_routing_code_is_rejected()
    {
        var client = AdminA();
        var code = UniqueCode("R");
        (await client.PostAsJsonAsync(RoutingsUrl, NewRouting(code))).StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(RoutingsUrl, NewRouting(code));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TenantAdminA_cannot_read_routing_of_tenant_B()
    {
        var created = (await (await AdminB().PostAsJsonAsync(RoutingsUrl, NewRouting(UniqueCode("R"))))
            .Content.ReadFromJsonAsync<ApiResponse<RoutingProbe>>())!.Data!;

        var response = await AdminA().GetAsync($"{RoutingsUrl}/{created.Id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantAdminA_cannot_activate_routing_of_tenant_B()
    {
        var created = (await (await AdminB().PostAsJsonAsync(RoutingsUrl, NewRouting(UniqueCode("R"))))
            .Content.ReadFromJsonAsync<ApiResponse<RoutingProbe>>())!.Data!;

        var response = await AdminA().PostAsync($"{RoutingsUrl}/{created.Id}/activate", null);

        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_read_work_center_roundtrip()
    {
        var client = AdminA();
        var code = UniqueCode("WC");

        var create = await client.PostAsJsonAsync(WorkCentersUrl, new
        {
            code,
            name = "Kesim",
            dailyCapacityMinutes = 480m,
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<ApiResponse<WorkCenterProbe>>())!.Data!;
        created.IsActive.Should().BeTrue();

        var list = await client.GetAsync(WorkCentersUrl);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TenantAdminA_cannot_read_work_center_of_tenant_B()
    {
        var created = (await (await AdminB().PostAsJsonAsync(WorkCentersUrl, new
        {
            code = UniqueCode("WC"),
            name = "Kesim",
            dailyCapacityMinutes = 480m,
        })).Content.ReadFromJsonAsync<ApiResponse<WorkCenterProbe>>())!.Data!;

        var response = await AdminA().GetAsync($"{WorkCentersUrl}/{created.Id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }
}
