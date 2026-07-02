using System.Net;
using System.Net.Http.Json;
using CoreAlign.Application.Common;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class DunningSettingsIntegrationTests
{
    private const string Url = "/api/v1/dunning-settings";

    private readonly CoreAlignWebApiFactory _factory;

    public DunningSettingsIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record SettingProbe(
        string Type,
        bool IsEnabled,
        bool SendInApp,
        bool SendEmail,
        List<Guid> RecipientUserIds);

    [Fact]
    public async Task Listing_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync(Url);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upsert_requires_tenant_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PutAsJsonAsync(Url, new
        {
            type = "InvoiceDueReminder",
            isEnabled = true,
            sendInApp = true,
            sendEmail = false,
            recipientUserIds = new List<Guid>(),
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Listing_returns_all_three_types_with_defaults()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var body = await (await client.GetAsync(Url)).Content.ReadFromJsonAsync<ApiResponse<List<SettingProbe>>>();

        body!.Data!.Select(s => s.Type).Should().BeEquivalentTo(
            new[] { "InvoiceDueReminder", "QuoteExpiringReminder", "StockCriticalReminder" });
    }

    [Fact]
    public async Task Upsert_persists_enabled_setting_with_recipients()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var recipient = Guid.NewGuid();

        var upsert = await client.PutAsJsonAsync(Url, new
        {
            type = "InvoiceDueReminder",
            isEnabled = true,
            sendInApp = true,
            sendEmail = true,
            recipientUserIds = new List<Guid> { recipient },
        });
        upsert.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await (await client.GetAsync(Url)).Content.ReadFromJsonAsync<ApiResponse<List<SettingProbe>>>();
        var invoiceDue = list!.Data!.Single(s => s.Type == "InvoiceDueReminder");
        invoiceDue.IsEnabled.Should().BeTrue();
        invoiceDue.SendEmail.Should().BeTrue();
        invoiceDue.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipient);
    }

    [Fact]
    public async Task Enabling_with_no_channel_is_rejected()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.PutAsJsonAsync(Url, new
        {
            type = "StockCriticalReminder",
            isEnabled = true,
            sendInApp = false,
            sendEmail = false,
            recipientUserIds = new List<Guid>(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Tenant_does_not_see_another_tenants_dunning_config()
    {
        var tenantA = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        await tenantA.PutAsJsonAsync(Url, new
        {
            type = "QuoteExpiringReminder",
            isEnabled = true,
            sendInApp = true,
            sendEmail = false,
            recipientUserIds = new List<Guid>(),
        });

        var tenantB = _factory.CreateClient().AuthenticatedAs(_factory.TenantB, TestPersona.TenantAdmin);
        var list = await (await tenantB.GetAsync(Url)).Content.ReadFromJsonAsync<ApiResponse<List<SettingProbe>>>();
        var quoteExpiring = list!.Data!.Single(s => s.Type == "QuoteExpiringReminder");
        quoteExpiring.IsEnabled.Should().BeFalse("TenantB must see its own default, not TenantA's enabled config");
    }
}
