using System.Net;
using System.Net.Http.Json;
using CoreAlign.Application.Common;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class BankAccountsIntegrationTests
{
    private const string BaseUrl = "/api/v1/master-data/bank-accounts";

    private readonly CoreAlignWebApiFactory _factory;

    public BankAccountsIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record BankAccountProbe(
        Guid Id,
        string AccountName,
        string BankName,
        string Iban,
        string Currency,
        decimal OpeningBalance,
        bool IsPrimary,
        bool IsActive);

    private static object NewAccount(string iban, bool isPrimary = false) => new
    {
        accountName = "Main Operating Account",
        bankName = "Türkiye İş Bankası",
        iban,
        currency = "TRY",
        openingBalance = 15000.50m,
        branchName = "Kadıköy",
        swift = "ISBKTRIS",
        isPrimary,
        notes = (string?)null,
    };

    [Fact]
    public async Task Listing_requires_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Creating_requires_tenant_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PostAsJsonAsync(BaseUrl, NewAccount("TR330006100519786457841326"));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Crud_roundtrip_normalizes_iban_and_persists_money()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var create = await client.PostAsJsonAsync(BaseUrl, NewAccount("tr02 0006 1005 1978 6457 8413 99", isPrimary: true));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<ApiResponse<BankAccountProbe>>())!.Data!;
        created.Iban.Should().Be("TR020006100519786457841399", "iban is normalized (uppercased, spaces stripped)");
        created.OpeningBalance.Should().Be(15000.50m);
        created.IsPrimary.Should().BeTrue();

        var get = await client.GetAsync($"{BaseUrl}/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", new
        {
            id = created.Id,
            accountName = "Renamed Account",
            bankName = "Garanti BBVA",
            iban = "TR020006100519786457841399",
            currency = "USD",
            openingBalance = 0m,
            branchName = (string?)null,
            swift = (string?)null,
            isPrimary = false,
            isActive = true,
            notes = "moved",
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<ApiResponse<BankAccountProbe>>())!.Data!;
        updated.AccountName.Should().Be("Renamed Account");
        updated.Currency.Should().Be("USD");

        var delete = await client.DeleteAsync($"{BaseUrl}/{created.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var getAfterDelete = await client.GetAsync($"{BaseUrl}/{created.Id}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tenant_cannot_read_another_tenants_bank_account()
    {
        var tenantAClient = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var create = await tenantAClient.PostAsJsonAsync(BaseUrl, NewAccount("TR330006100519786457841326"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<BankAccountProbe>>())!.Data!.Id;

        var tenantBClient = _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);
        var crossTenant = await tenantBClient.GetAsync($"{BaseUrl}/{id}");
        crossTenant.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
