using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.AiHelper.Gemini;

public sealed class GeminiChatProvider : IAiChatProvider
{
    public const string HttpClientName = "GeminiChat";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiHelperOptions _options;
    private readonly ILogger<GeminiChatProvider> _logger;

    public GeminiChatProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiHelperOptions> options,
        ILogger<GeminiChatProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "gemini";

    public bool SupportsTools => true;

    public IAsyncEnumerable<AiChatDelta> StreamAsync(AiChatRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return StreamInternalAsync(request, ct);
    }

    private async IAsyncEnumerable<AiChatDelta> StreamInternalAsync(
        AiChatRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var completion = await CompleteAsync(request, ct).ConfigureAwait(false);
        if (completion.Text.Length > 0)
        {
            yield return new AiChatDelta(completion.Text, false);
        }
        yield return new AiChatDelta(string.Empty, true);
    }

    public async Task<AiChatCompletion> CompleteAsync(AiChatRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("AiHelper:ApiKey is not configured for the Gemini provider.");
        }

        var payload = BuildPayload(request);

        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{_options.ChatModel}:generateContent")
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);

        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini generateContent failed with status {Status}", (int)response.StatusCode);
            throw new HttpRequestException($"Gemini request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        return ParseCompletion(doc);
    }

    private JsonObject BuildPayload(AiChatRequest request)
    {
        var root = new JsonObject();

        var systemText = string.Join(
            "\n\n",
            request.Messages.Where(m => m.Role == AiChatRole.System).Select(m => m.Content));
        if (!string.IsNullOrWhiteSpace(systemText))
        {
            root["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = systemText })
            };
        }

        var contents = new JsonArray();
        foreach (var message in request.Messages)
        {
            switch (message.Role)
            {
                case AiChatRole.System:
                    continue;
                case AiChatRole.Tool:
                    contents.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = new JsonArray(new JsonObject
                        {
                            ["functionResponse"] = new JsonObject
                            {
                                ["name"] = message.ToolCallId ?? "tool",
                                ["response"] = new JsonObject { ["result"] = ParseToNode(message.Content) }
                            }
                        })
                    });
                    break;
                default:
                    var parts = new JsonArray();
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        parts.Add(new JsonObject { ["text"] = message.Content });
                    }
                    if (message.ToolCalls is { Count: > 0 })
                    {
                        foreach (var call in message.ToolCalls)
                        {
                            parts.Add(new JsonObject
                            {
                                ["functionCall"] = new JsonObject
                                {
                                    ["name"] = call.Name,
                                    ["args"] = ParseToNode(call.ArgumentsJson) ?? new JsonObject()
                                }
                            });
                        }
                    }
                    contents.Add(new JsonObject
                    {
                        ["role"] = message.Role == AiChatRole.Assistant ? "model" : "user",
                        ["parts"] = parts
                    });
                    break;
            }
        }

        root["contents"] = contents;

        if (request.Tools is { Count: > 0 })
        {
            var declarations = new JsonArray();
            foreach (var tool in request.Tools)
            {
                declarations.Add(new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = ParseToNode(tool.ParametersJsonSchema) ?? new JsonObject()
                });
            }
            root["tools"] = new JsonArray(new JsonObject { ["functionDeclarations"] = declarations });
        }

        root["generationConfig"] = new JsonObject
        {
            ["temperature"] = request.Temperature ?? _options.Temperature,
            ["maxOutputTokens"] = request.MaxOutputTokens ?? _options.MaxOutputTokens
        };

        return root;
    }

    private static AiChatCompletion ParseCompletion(JsonDocument doc)
    {
        var text = new StringBuilder();
        var toolCalls = new List<AiToolCall>();

        if (doc.RootElement.TryGetProperty("candidates", out var candidates)
            && candidates.ValueKind == JsonValueKind.Array
            && candidates.GetArrayLength() > 0)
        {
            var candidate = candidates[0];
            if (candidate.TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts)
                && parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textPart) && textPart.ValueKind == JsonValueKind.String)
                    {
                        text.Append(textPart.GetString());
                    }
                    else if (part.TryGetProperty("functionCall", out var functionCall))
                    {
                        var name = functionCall.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                        if (!string.IsNullOrEmpty(name))
                        {
                            var args = functionCall.TryGetProperty("args", out var argsEl)
                                ? argsEl.GetRawText()
                                : "{}";
                            toolCalls.Add(new AiToolCall(name, name, args));
                        }
                    }
                }
            }
        }

        return new AiChatCompletion(text.ToString(), toolCalls);
    }

    private static JsonNode? ParseToNode(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return JsonValue.Create(json);
        }
    }
}
