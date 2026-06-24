using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.AiHelper.Ollama;

public sealed class OllamaChatProvider : IAiChatProvider
{
    public const string HttpClientName = "OllamaChat";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiHelperOptions _options;
    private readonly ILogger<OllamaChatProvider> _logger;

    public OllamaChatProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiHelperOptions> options,
        ILogger<OllamaChatProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "ollama";

    public bool SupportsTools => false;

    public IAsyncEnumerable<AiChatDelta> StreamAsync(AiChatRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return StreamInternalAsync(request, ct);
    }

    public async Task<AiChatCompletion> CompleteAsync(AiChatRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var builder = new StringBuilder();
        await foreach (var delta in StreamInternalAsync(request, ct).ConfigureAwait(false))
        {
            if (delta.Done)
            {
                break;
            }
            builder.Append(delta.Content);
        }
        return new AiChatCompletion(builder.ToString(), Array.Empty<AiToolCall>());
    }

    private async IAsyncEnumerable<AiChatDelta> StreamInternalAsync(
        AiChatRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var payload = new OllamaChatRequest
        {
            Model = _options.ChatModel,
            Stream = true,
            Messages = request.Messages
                .Select(m => new OllamaChatMessage
                {
                    Role = m.Role switch
                    {
                        AiChatRole.System => "system",
                        AiChatRole.Assistant => "assistant",
                        _ => "user"
                    },
                    Content = m.Content
                })
                .ToList(),
            Options = new OllamaGenerationOptions
            {
                Temperature = request.Temperature ?? _options.Temperature,
                NumPredict = request.MaxOutputTokens ?? _options.MaxOutputTokens,
                NumThread = _options.NumThreads > 0 ? _options.NumThreads : null
            }
        };

        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await client
            .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, SerializerOptions);
            if (chunk is null)
            {
                _logger.LogWarning("Ollama chat stream returned an unparseable line");
                continue;
            }

            var content = chunk.Message?.Content ?? string.Empty;
            if (content.Length > 0)
            {
                yield return new AiChatDelta(content, false);
            }

            if (chunk.Done)
            {
                yield return new AiChatDelta(string.Empty, true);
                yield break;
            }
        }
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;
        public bool Stream { get; set; }
        public List<OllamaChatMessage> Messages { get; set; } = new();
        public OllamaGenerationOptions? Options { get; set; }
    }

    private sealed class OllamaChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaGenerationOptions
    {
        public double Temperature { get; set; }
        public int NumPredict { get; set; }
        public int? NumThread { get; set; }
    }

    private sealed record OllamaChatResponse(OllamaChatMessage? Message, bool Done);
}
