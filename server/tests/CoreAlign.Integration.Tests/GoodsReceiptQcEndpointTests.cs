using System.Net;
using System.Net.Http.Json;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class GoodsReceiptQcEndpointTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public GoodsReceiptQcEndpointTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private static string ApproveUrl(Guid id) => $"/api/v1/purchase-orders/goods-receipts/{id}/qc/approve";
    private static string RejectUrl(Guid id) => $"/api/v1/purchase-orders/goods-receipts/{id}/qc/reject";

    [Fact]
    public async Task Approve_qc_requires_authentication()
    {
        var response = await _factory.CreateClient().PostAsync(ApproveUrl(Guid.NewGuid()), content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Approve_qc_is_forbidden_for_non_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PostAsync(ApproveUrl(Guid.NewGuid()), content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reject_qc_is_forbidden_for_non_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PostAsJsonAsync(RejectUrl(Guid.NewGuid()), new { reason = "x" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_qc_on_unknown_id_is_not_found_for_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.PostAsync(ApproveUrl(Guid.NewGuid()), content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
