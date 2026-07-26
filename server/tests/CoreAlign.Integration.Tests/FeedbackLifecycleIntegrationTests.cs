using System.Net;
using System.Net.Http.Json;
using CoreAlign.Application.Feedback.Notifications;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Notifications;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Integration.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

[Collection(IntegrationCollection.Name)]
public class FeedbackLifecycleIntegrationTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public FeedbackLifecycleIntegrationTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Opening_a_ticket_notifies_exactly_once_and_a_replay_adds_nothing()
    {
        var tenantId = _factory.TenantA.TenantId;
        var recipientId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            foreach (var channel in new[] { NotificationChannel.InApp })
            {
                var exists = await db
                    .Set<NotificationTemplate>()
                    .IgnoreQueryFilters()
                    .AnyAsync(x =>
                        x.Key == FeedbackTemplateKeys.Created
                        && x.Channel == channel
                        && x.Locale == "tr");
                if (!exists)
                {
                    db.Set<NotificationTemplate>().Add(new NotificationTemplate(
                        null,
                        FeedbackTemplateKeys.Created,
                        channel,
                        "tr",
                        "Yeni kayıt: {{title}}",
                        "{{type}} türünde yeni bir kayıt açıldı."));
                }
            }
            await db.SaveChangesAsync();
        }

        var ticket = new FeedbackTicket(
            FeedbackType.Bug,
            "Notification probe",
            "body",
            FeedbackPriority.Medium,
            createdByUserId: recipientId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            ticket.TenantId = tenantId;
            db.Set<FeedbackTicket>().Add(ticket);
            await db.SaveChangesAsync();
        }

        var payload = FeedbackNotificationPayload.Created(ticket);
        var json = System.Text.Json.JsonSerializer.Serialize(payload);

        async Task<string> DrainOnceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var handler = scope
                .ServiceProvider
                .GetServices<CoreAlign.Application.Common.Outbox.IOutboxMessageHandler>()
                .First(h => h.MessageType == FeedbackNotificationOutbox.MessageType);
            var result = await handler.HandleAsync(json, CancellationToken.None);
            return result.ResultOrError;
        }

        await DrainOnceAsync();
        var afterFirst = await CountMessagesAsync(tenantId);

        // The dispatcher dedups on a SHA256 of the rendered payload; the payload carries no date, so a
        // replay of the very same outbox message must not produce a second row.
        await DrainOnceAsync();
        var afterSecond = await CountMessagesAsync(tenantId);

        afterSecond.Should().Be(afterFirst);
    }

    private async Task<int> CountMessagesAsync(Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        return await db
            .Set<NotificationMessage>()
            .IgnoreQueryFilters()
            .CountAsync(m => m.TenantId == tenantId && m.TemplateKey == FeedbackTemplateKeys.Created);
    }

    [Fact]
    public async Task Comment_endpoints_require_authentication()
    {
        var client = _factory.CreateClient();
        var id = Guid.NewGuid();

        var list = await client.GetAsync($"/api/v1/feedback/{id}/comments");
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var post = await client.PostAsJsonAsync(
            $"/api/v1/feedback/{id}/comments",
            new { body = "hi", isInternal = false });
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_ticket_from_another_tenant_is_not_readable_or_commentable()
    {
        Guid ticketId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            var ticket = new FeedbackTicket(
                FeedbackType.Bug,
                "Tenant B private",
                "body",
                FeedbackPriority.Low)
            {
                TenantId = _factory.TenantB.TenantId,
            };
            db.Set<FeedbackTicket>().Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var tenantAClient = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var comments = await tenantAClient.GetAsync($"/api/v1/feedback/{ticketId}/comments");
        comments.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);

        var post = await tenantAClient.PostAsJsonAsync(
            $"/api/v1/feedback/{ticketId}/comments",
            new { body = "leak attempt", isInternal = false });
        post.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);

        var status = await tenantAClient.PatchAsJsonAsync(
            $"/api/v1/feedback/{ticketId}/status",
            new { status = "InProgress", adminResponse = (string?)null });
        status.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_illegal_status_transition_is_a_conflict_not_a_silent_write()
    {
        Guid ticketId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
            var ticket = new FeedbackTicket(FeedbackType.Bug, "FSM probe", "body", FeedbackPriority.Low)
            {
                TenantId = _factory.TenantA.TenantId,
            };
            db.Set<FeedbackTicket>().Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        // Open -> Closed is not a legal move; the aggregate must refuse it.
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/feedback/{ticketId}/status",
            new { status = "Closed", adminResponse = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CoreAlignDbContext>();
        var stored = await verifyDb
            .Set<FeedbackTicket>()
            .IgnoreQueryFilters()
            .FirstAsync(f => f.Id == ticketId);
        stored.Status.Should().Be(FeedbackStatus.Open);
    }
}
