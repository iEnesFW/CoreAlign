using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CoreAlign.Application.Common.Email;

namespace CoreAlign.Infrastructure.Services;

public sealed partial class SafeEmailRenderer : IEmailRenderer
{
    private static readonly Regex PlaceholderPattern = BuildPlaceholderPattern();

    public RenderedEmail Render(string subjectTemplate, string bodyTemplate, IReadOnlyDictionary<string, object?> context)
    {
        var resolver = new ContextResolver(context);
        var subject = Substitute(subjectTemplate ?? string.Empty, resolver, escapeHtml: false);
        var body = Substitute(bodyTemplate ?? string.Empty, resolver, escapeHtml: true);
        return new RenderedEmail(subject, body);
    }

    private static string Substitute(string source, ContextResolver resolver, bool escapeHtml)
    {
        if (source.Length == 0) return source;

        return PlaceholderPattern.Replace(source, match =>
        {
            var path = match.Groups[1].Value;
            var raw = resolver.Resolve(path);
            if (raw is null) return string.Empty;
            var text = raw is IFormattable formattable
                ? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
                : raw.ToString() ?? string.Empty;
            return escapeHtml ? System.Net.WebUtility.HtmlEncode(text) : text;
        });
    }

    [GeneratedRegex(@"\{\{\s*([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex BuildPlaceholderPattern();

    private sealed class ContextResolver
    {
        private readonly IReadOnlyDictionary<string, object?> _root;

        public ContextResolver(IReadOnlyDictionary<string, object?> root)
        {
            _root = root ?? new Dictionary<string, object?>(0, StringComparer.OrdinalIgnoreCase);
        }

        public object? Resolve(string dottedPath)
        {
            var segments = dottedPath.Split('.');
            object? current = _root;
            foreach (var segment in segments)
            {
                current = StepInto(current, segment);
                if (current is null) return null;
            }
            return current;
        }

        private static object? StepInto(object? current, string segment)
        {
            if (current is null) return null;

            if (current is IReadOnlyDictionary<string, object?> ro)
            {
                return TryGetCaseInsensitive(ro, segment);
            }
            if (current is IDictionary<string, object?> dict)
            {
                foreach (var kv in dict)
                {
                    if (string.Equals(kv.Key, segment, StringComparison.OrdinalIgnoreCase))
                        return kv.Value;
                }
                return null;
            }
            if (current is JsonElement je)
            {
                if (je.ValueKind != JsonValueKind.Object) return null;
                foreach (var prop in je.EnumerateObject())
                {
                    if (string.Equals(prop.Name, segment, StringComparison.OrdinalIgnoreCase))
                        return UnwrapJsonElement(prop.Value);
                }
                return null;
            }
            return null;
        }

        private static object? TryGetCaseInsensitive(IReadOnlyDictionary<string, object?> dict, string key)
        {
            if (dict.TryGetValue(key, out var direct)) return direct;
            foreach (var kv in dict)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        private static object? UnwrapJsonElement(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => (object)element,
        };
    }
}
