using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Warranty;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class MyWarrantyContractsControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public MyWarrantyContractsControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_caller_receives_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/customer-portal/warranty-contracts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_customer_only_sees_own_warranty_contracts()
    {
        var ownContractId = await SeedWarrantyContractAsync(_factory.TenantA);
        var otherContractId = await SeedWarrantyContractAsync(_factory.TenantB);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync("/api/v1/customer-portal/warranty-contracts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<WarrantyContractDto>>>(JsonOptions);
        body.Should().NotBeNull();
        body!.IsSuccess.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Select(c => c.Id).Should().Contain(ownContractId);
        body.Data!.Select(c => c.Id).Should().NotContain(otherContractId);
    }

    [Fact]
    public async Task Cross_customer_access_returns_404()
    {
        var otherContractId = await SeedWarrantyContractAsync(_factory.TenantB);

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync($"/api/v1/customer-portal/warranty-contracts/{otherContractId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dealer_persona_cannot_access_customer_portal_warranty_endpoint()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Dealer);

        var response = await client.GetAsync("/api/v1/customer-portal/warranty-contracts");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedWarrantyContractAsync(TenantFixture tenant)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var service = sp.GetRequiredService<IWarrantyContractService>();

        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            var contract = await service.CreateAsync(
                orderId: tenant.OrderId,
                customerId: tenant.CustomerId,
                coverageType: WarrantyCoverageType.FullService,
                warrantyMonths: 12,
                termsJson: "{}");
            await db.SaveChangesAsync();
            return contract.Id;
        }
    }
}
