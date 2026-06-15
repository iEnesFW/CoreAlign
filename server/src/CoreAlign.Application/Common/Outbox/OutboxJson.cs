using System.Text.Json;

namespace CoreAlign.Application.Common.Outbox;

/// <summary>Shared serializer settings so outbox payloads round-trip identically.</summary>
internal static class OutboxJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
