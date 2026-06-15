using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications.Templates;

public interface INotificationTemplateRenderer
{
    Task<RenderedTemplate> RenderAsync(
        Guid? tenantId,
        string templateKey,
        NotificationChannel channel,
        string locale,
        object payload,
        CancellationToken ct = default);
}

public sealed record RenderedTemplate(string? Subject, string BodyHtml, string BodyText);
