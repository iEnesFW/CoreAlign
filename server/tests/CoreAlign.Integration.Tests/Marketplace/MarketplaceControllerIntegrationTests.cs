using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.GlassEnclosure.Marketplace.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests.Marketplace;

[Collection(IntegrationCollection.Name)]
public class MarketplaceControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CoreAlignWebApiFactory _factory;

    public MarketplaceControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Listing_unauthenticated_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/marketplace/templates");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Listing_returns_published_marketplace_templates()
    {
        var publishedId = await SeedPublishedMarketplaceTemplateAsync("MKT-LIST-01");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync("/api/v1/marketplace/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<MarketplaceTemplateSummaryDto>>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Data.Should().Contain(d => d.Id == publishedId);
    }

    [Fact]
    public async Task Install_clones_published_template_into_caller_tenant()
    {
        var publishedId = await SeedPublishedMarketplaceTemplateAsync("MKT-INSTALL-01");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.PostAsync($"/api/v1/marketplace/templates/{publishedId}/install", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<InstallMarketplaceResultDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Data!.InstalledTemplateId.Should().NotBe(publishedId);
        envelope.Data.InstalledTemplateId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_for_non_published_template_returns_null_payload()
    {
        var draftId = await SeedTenantOnlyTemplateAsync("MKT-DRAFT-01");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var response = await client.GetAsync($"/api/v1/marketplace/templates/{draftId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<MarketplaceTemplateDetailDto>>(JsonOptions);
        envelope!.Data.Should().BeNull();
    }

    private async Task<Guid> SeedPublishedMarketplaceTemplateAsync(string code)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();

        var publisherTenant = _factory.TenantB.TenantId;
        using (TenantContextAccessor.PushTenant(publisherTenant))
        {
            var template = new ProjectTemplate(
                code: $"{code}-{Guid.NewGuid():N}",
                displayNameKey: "Marketplace.IT.Test",
                isSystemTemplate: false,
                category: EnclosureCategory.Vertical,
                subtype: EnclosureSubtype.Balcony,
                geometryMode: GeometryMode.Planar,
                mountingTopology: MountingTopology.ProfileFramed,
                defaultConnectorKind: ConnectorKind.Profile)
            {
                TenantId = publisherTenant,
            };
            template.SubmitToMarketplace(publisherTenant);
            template.Publish(Guid.NewGuid());
            db.ProjectTemplates.Add(template);
            await db.SaveChangesAsync();
            return template.Id;
        }
    }

    private async Task<Guid> SeedTenantOnlyTemplateAsync(string code)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var template = new ProjectTemplate(
                code: $"{code}-{Guid.NewGuid():N}",
                displayNameKey: "Marketplace.IT.Draft",
                isSystemTemplate: false,
                category: EnclosureCategory.Vertical,
                subtype: EnclosureSubtype.Balcony,
                geometryMode: GeometryMode.Planar,
                mountingTopology: MountingTopology.ProfileFramed,
                defaultConnectorKind: ConnectorKind.Profile)
            {
                TenantId = _factory.TenantA.TenantId,
            };
            db.ProjectTemplates.Add(template);
            await db.SaveChangesAsync();
            return template.Id;
        }
    }
}
