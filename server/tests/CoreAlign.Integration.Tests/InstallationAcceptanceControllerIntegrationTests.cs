using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoreAlign.Application.Common;
using CoreAlign.Application.Common.Storage;
using CoreAlign.Application.Installation;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class InstallationAcceptanceControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public InstallationAcceptanceControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_start_creates_acceptance_and_returns_201()
    {
        var (workOrderId, _) = await SeedProjectAndWorkOrderAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var cmd = new StartInstallationAcceptanceCommand(workOrderId, _factory.TenantA.TenantAdminUserId);

        var response = await client.PostAsJsonAsync("/api/v1/installation-acceptances/start", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<InstallationAcceptanceDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.WorkOrderId.Should().Be(workOrderId);
        body.Data.Status.Should().Be(InstallationAcceptanceStatus.Draft);
        body.Data.InspectorUserId.Should().Be(_factory.TenantA.TenantAdminUserId);
    }

    [Fact]
    public async Task Patch_checklist_updates_item_and_returns_200()
    {
        var (workOrderId, _) = await SeedProjectAndWorkOrderAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var acceptanceId = await StartAcceptanceAsync(client, workOrderId);

        var patchCmd = new UpdateChecklistItemCommand(
            AcceptanceId: acceptanceId,
            Category: "Glass",
            ItemKey: "Glass.NoChips",
            Result: InstallationChecklistResult.Pass,
            Notes: "Looks good");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/installation-acceptances/{acceptanceId}/checklist", patchCmd, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<InstallationAcceptanceDto>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.ChecklistJson.Should().Contain("\"result\":\"Pass\"");
    }

    [Fact]
    public async Task Post_signature_sets_signature_file_and_returns_200()
    {
        var (workOrderId, _) = await SeedProjectAndWorkOrderAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var acceptanceId = await StartAcceptanceAsync(client, workOrderId);

        var fileId = await SeedAcceptanceFileAsync(acceptanceId);
        var signatureCmd = new CaptureCustomerSignatureCommand(
            AcceptanceId: acceptanceId,
            FileId: fileId,
            CustomerName: "John Customer");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/installation-acceptances/{acceptanceId}/signature", signatureCmd);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<InstallationAcceptanceDto>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.CustomerSignatureFileId.Should().Be(fileId);
        body.Data.CustomerName.Should().Be("John Customer");
        body.Data.Status.Should().Be(InstallationAcceptanceStatus.SignedByCustomer);
    }

    [Fact]
    public async Task Post_accept_marks_status_accepted()
    {
        var (workOrderId, _) = await SeedProjectAndWorkOrderAsync();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);
        var acceptanceId = await StartAcceptanceAsync(client, workOrderId);

        var signatureCmd = new CaptureCustomerSignatureCommand(
            AcceptanceId: acceptanceId,
            FileId: await SeedAcceptanceFileAsync(acceptanceId),
            CustomerName: "John Customer");
        var signatureResponse = await client.PostAsJsonAsync(
            $"/api/v1/installation-acceptances/{acceptanceId}/signature", signatureCmd);
        signatureResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var acceptCmd = new AcceptInstallationCommand(
            AcceptanceId: acceptanceId,
            IdempotencyKey: Guid.NewGuid().ToString("N"));
        var response = await client.PostAsJsonAsync(
            $"/api/v1/installation-acceptances/{acceptanceId}/accept",
            acceptCmd);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<InstallationAcceptanceDto>>(JsonOptions);
        body!.Data.Should().NotBeNull();
        body.Data!.Status.Should().Be(InstallationAcceptanceStatus.Accepted);
        body.Data.CompletedAtUtc.Should().NotBeNull();
    }

    private async Task<Guid> SeedAcceptanceFileAsync(Guid acceptanceId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            await using var content = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
            var stored = await storage.SaveAsync(
                $"installation-acceptance/{acceptanceId:N}",
                "signature.png",
                content,
                "image/png");
            var fileName = stored.RelativePath[(stored.RelativePath.LastIndexOf('/') + 1)..];
            return Guid.ParseExact(fileName[..fileName.IndexOf('_')], "N");
        }
    }

    private async Task<Guid> StartAcceptanceAsync(HttpClient client, Guid workOrderId)
    {
        var cmd = new StartInstallationAcceptanceCommand(workOrderId, _factory.TenantA.TenantAdminUserId);
        var response = await client.PostAsJsonAsync("/api/v1/installation-acceptances/start", cmd);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<InstallationAcceptanceDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    private async Task<(Guid workOrderId, Guid projectId)> SeedProjectAndWorkOrderAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var project = new GlassProject(
                code: $"IA-{Guid.NewGuid():N}"[..12],
                customerId: _factory.TenantA.CustomerId,
                projectName: "Installation Acceptance Test Project",
                createdByUserId: _factory.TenantA.TenantAdminUserId)
            {
                TenantId = _factory.TenantA.TenantId,
            };
            db.GlassProjects.Add(project);
            await db.SaveChangesAsync();

            var workOrder = new GlassWorkOrder(
                projectId: project.Id,
                scheduledStartDate: DateTime.UtcNow,
                scheduledEndDate: DateTime.UtcNow.AddDays(1),
                workloadM2: 25m)
            {
                TenantId = _factory.TenantA.TenantId,
            };
            db.GlassWorkOrders.Add(workOrder);
            await db.SaveChangesAsync();

            return (workOrder.Id, project.Id);
        }
    }
}
