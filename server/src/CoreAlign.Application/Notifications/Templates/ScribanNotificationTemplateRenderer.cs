using System.Reflection;
using System.Text;
using System.Text.Json;
using CoreAlign.Application.Notifications.Repositories;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Notifications.Templates;

public sealed class ScribanNotificationTemplateRenderer : INotificationTemplateRenderer
{
    private readonly INotificationTemplateRepository _templates;

    public ScribanNotificationTemplateRenderer(INotificationTemplateRepository templates)
    {
        _templates = templates;
    }

    public async Task<RenderedTemplate> RenderAsync(
        Guid? tenantId,
        string templateKey,
        NotificationChannel channel,
        string locale,
        object payload,
        CancellationToken ct = default)
    {
        var template = await ResolveTemplateAsync(tenantId, templateKey, channel, locale, ct).ConfigureAwait(false);
        if (template is null) throw new TemplateNotFoundException(templateKey, locale);

        var values = NormalizePayload(payload);
        var bodyHtml = Substitute(template.BodyTemplate, values);
        var subject = template.Subject is null ? null : Substitute(template.Subject, values);
        var bodyText = StripHtml(bodyHtml);
        return new RenderedTemplate(subject, bodyHtml, bodyText);
    }

    private async Task<Domain.Entities.Notifications.NotificationTemplate?> ResolveTemplateAsync(
        Guid? tenantId,
        string templateKey,
        NotificationChannel channel,
        string locale,
        CancellationToken ct)
    {
        if (tenantId.HasValue)
        {
            var tenant = await _templates.GetByKeyLocaleAsync(tenantId, templateKey, channel, locale, ct).ConfigureAwait(false);
            if (tenant is not null) return tenant;
        }
        var global = await _templates.GetByKeyLocaleAsync(null, templateKey, channel, locale, ct).ConfigureAwait(false);
        if (global is not null) return global;

        var fallback = await _templates.GetByKeyLocaleAsync(null, templateKey, channel, "en", ct).ConfigureAwait(false);
        return fallback;
    }

    private static Dictionary<string, string> NormalizePayload(object payload)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (payload is null) return dict;

        if (payload is IDictionary<string, object?> generic)
        {
            foreach (var kv in generic)
            {
                dict[kv.Key] = kv.Value?.ToString() ?? string.Empty;
            }
            return dict;
        }

        if (payload is IDictionary<string, string> stringDict)
        {
            foreach (var kv in stringDict) dict[kv.Key] = kv.Value ?? string.Empty;
            return dict;
        }

        if (payload is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in jsonElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString() ?? string.Empty
                    : prop.Value.ToString();
            }
            return dict;
        }

        var type = payload.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(payload);
            dict[prop.Name] = value?.ToString() ?? string.Empty;
        }
        return dict;
    }

    private static string Substitute(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template)) return template ?? string.Empty;
        var sb = new StringBuilder(template.Length);
        var i = 0;
        while (i < template.Length)
        {
            if (i + 1 < template.Length && template[i] == '{' && template[i + 1] == '{')
            {
                var close = template.IndexOf("}}", i + 2, StringComparison.Ordinal);
                if (close > 0)
                {
                    var key = template[(i + 2)..close].Trim();
                    sb.Append(values.TryGetValue(key, out var v) ? v : string.Empty);
                    i = close + 2;
                    continue;
                }
            }
            sb.Append(template[i]);
            i++;
        }
        return sb.ToString();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var sb = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var ch in html)
        {
            if (ch == '<') { inTag = true; continue; }
            if (ch == '>') { inTag = false; continue; }
            if (!inTag) sb.Append(ch);
        }
        return sb.ToString();
    }
}
