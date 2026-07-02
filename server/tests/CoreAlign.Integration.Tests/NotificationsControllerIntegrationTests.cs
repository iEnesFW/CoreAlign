using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class NotificationsControllerIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public NotificationsControllerIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_me_returns_200_and_only_returns_caller_own_notifications()
    {
        var ownId = await SeedNotificationMessageAsync(_factory.TenantA, _factory.TenantA.CustomerUserId, "Subject-Own");
        await SeedNotificationMessageAsync(_factory.TenantA, _factory.TenantA.DealerUserId, "Subject-Other");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync("/api/v1/notification-messages/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var ids = doc.RootElement.EnumerateArray()
            .Select(el => el.GetProperty("id").GetGuid())
            .ToList();
        ids.Should().Contain(ownId);
        var subjects = doc.RootElement.EnumerateArray()
            .Select(el => el.GetProperty("subject").GetString())
            .ToList();
        subjects.Should().NotContain("Subject-Other");
    }

    [Fact]
    public async Task Get_me_does_not_return_notifications_from_other_tenants()
    {
        await SeedNotificationMessageAsync(_factory.TenantB, _factory.TenantB.CustomerUserId, "Tenant-B-Subject");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync("/api/v1/notification-messages/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var subjects = doc.RootElement.EnumerateArray()
            .Select(el => el.GetProperty("subject").GetString())
            .ToList();
        subjects.Should().NotContain("Tenant-B-Subject");
    }

    [Fact]
    public async Task Post_mark_read_persists_read_state_for_recipient()
    {
        var id = await SeedNotificationMessageAsync(_factory.TenantA, _factory.TenantA.CustomerUserId, "Subject-MarkRead");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.PostAsync($"/api/v1/notification-messages/{id}/mark-read", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var msg = await db.NotificationMessages.IgnoreQueryFilters().FirstAsync(m => m.Id == id);
            msg.Status.Should().Be(NotificationStatus.Read);
            msg.ReadAtUtc.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Post_mark_read_returns_forbidden_when_caller_is_not_recipient()
    {
        var otherUserId = _factory.TenantA.DealerUserId;
        var id = await SeedNotificationMessageAsync(_factory.TenantA, otherUserId, "Subject-NotMine");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.PostAsync($"/api/v1/notification-messages/{id}/mark-read", content: null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_mark_read_returns_not_found_for_unknown_id()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.PostAsync(
            $"/api/v1/notification-messages/{Guid.NewGuid()}/mark-read", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_mark_read_cannot_touch_another_tenants_notification()
    {
        var id = await SeedNotificationMessageAsync(_factory.TenantB, _factory.TenantB.CustomerUserId, "Tenant-B-MarkRead");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.PostAsync($"/api/v1/notification-messages/{id}/mark-read", content: null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantB.TenantId))
        {
            var msg = await db.NotificationMessages.IgnoreQueryFilters().FirstAsync(m => m.Id == id);
            msg.Status.Should().NotBe(NotificationStatus.Read);
            msg.ReadAtUtc.Should().BeNull();
        }
    }

    [Fact]
    public async Task Get_admin_list_returns_200_for_tenant_admin()
    {
        await SeedNotificationMessageAsync(_factory.TenantA, _factory.TenantA.CustomerUserId, "Subject-Admin-List");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync("/api/v1/notification-messages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_admin_list_is_forbidden_for_non_admin_users()
    {
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);

        var response = await client.GetAsync("/api/v1/notification-messages");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_acknowledge_persists_note_and_actor_for_recipient()
    {
        var id = await SeedNotificationMessageAsync(_factory.TenantA, _factory.TenantA.CustomerUserId, "Subject-Ack");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/notification-messages/{id}/acknowledge",
            new { note = "Reviewed and approved" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        using (TenantContextAccessor.PushTenant(_factory.TenantA.TenantId))
        {
            var msg = await db.NotificationMessages.IgnoreQueryFilters().FirstAsync(m => m.Id == id);
            msg.AcknowledgedAtUtc.Should().NotBeNull();
            msg.AcknowledgmentNote.Should().Be("Reviewed and approved");
            msg.AcknowledgedByUserId.Should().Be(_factory.TenantA.CustomerUserId);
        }

        var listJson = await (await client.GetAsync("/api/v1/notification-messages/me")).Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listJson);
        var row = doc.RootElement.EnumerateArray().Single(el => el.GetProperty("id").GetGuid() == id);
        row.GetProperty("isAcknowledged").GetBoolean().Should().BeTrue();
        row.GetProperty("acknowledgmentNote").GetString().Should().Be("Reviewed and approved");
    }

    [Fact]
    public async Task Post_acknowledge_is_forbidden_when_caller_is_not_recipient()
    {
        var id = await SeedNotificationMessageAsync(_factory.TenantA, _factory.TenantA.DealerUserId, "Subject-NotMine-Ack");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/notification-messages/{id}/acknowledge",
            new { note = (string?)null });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_acknowledge_cannot_touch_another_tenants_notification()
    {
        var id = await SeedNotificationMessageAsync(_factory.TenantB, _factory.TenantB.CustomerUserId, "Tenant-B-Ack");

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.Customer);
        var response = await client.PostAsJsonAsync(
            $"/api/v1/notification-messages/{id}/acknowledge",
            new { note = "x" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    private async Task<Guid> SeedNotificationMessageAsync(TenantFixture tenant, Guid userId, string subject)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();

        using (TenantContextAccessor.PushTenant(tenant.TenantId))
        {
            var message = new NotificationMessage(
                tenantId: tenant.TenantId,
                userId: userId,
                customerId: null,
                channel: NotificationChannel.Email,
                templateKey: "Test.Notification",
                locale: "en",
                recipientAddress: $"user-{Guid.NewGuid():N}@test.local",
                categoryKey: "Test",
                subject: subject,
                bodyMarkdown: "Body",
                payloadJson: "{}");
            db.NotificationMessages.Add(message);
            await db.SaveChangesAsync();
            return message.Id;
        }
    }
}
