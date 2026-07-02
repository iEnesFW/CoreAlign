using System.Net;
using System.Net.Http.Json;
using CoreAlign.Application.Common;
using CoreAlign.Integration.Tests.Infrastructure;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class SettingsDocumentSequencesIntegrationTests
{
    private const string ListUrl = "/api/v1/settings/document-sequences";

    private readonly CoreAlignWebApiFactory _factory;

    public SettingsDocumentSequencesIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record SequenceProbe(string Type, string Prefix, string Preview, bool IsConfigured);

    [Fact]
    public async Task Listing_sequences_requires_authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(ListUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listing_sequences_returns_every_type_with_a_preview()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync(ListUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SequenceProbe>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();

        var order = body.Data!.FirstOrDefault(s => s.Type == "OrderNumber");
        order.Should().NotBeNull("the order-number sequence must be listed for the numbering settings + preview badge");
        order!.Preview.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Configuring_a_sequence_requires_tenant_admin()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.PostAsJsonAsync(
            ListUrl,
            new { type = "DebitNoteNumber", prefix = "ZZZ", padLength = 4, format = (string?)null, nextNumber = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Configuring_a_sequence_as_admin_updates_the_listed_prefix()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var configure = await client.PostAsJsonAsync(
            ListUrl,
            new { type = "DebitNoteNumber", prefix = "ZQX", padLength = 4, format = (string?)null, nextNumber = 1 });
        configure.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await client.GetAsync(ListUrl);
        var body = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<SequenceProbe>>>();
        var debitNote = body!.Data!.First(s => s.Type == "DebitNoteNumber");
        debitNote.Prefix.Should().Be("ZQX");
        debitNote.IsConfigured.Should().BeTrue();
        debitNote.Preview.Should().Contain("ZQX");
    }
}
