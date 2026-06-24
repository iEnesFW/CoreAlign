using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreAlign.Infrastructure.AiHelper.Ollama;

public sealed class OllamaEmbeddingProvider : IAiEmbeddingProvider
{
    public const string HttpClientName = "OllamaEmbedding";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiHelperOptions _options;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public OllamaEmbeddingProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AiHelperOptions> options,
        ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "ollama";

    public int Dimensions => _options.EmbeddingDimensions;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (inputs.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var payload = new OllamaEmbedRequest
        {
            Model = _options.EmbeddingModel,
            Input = inputs.ToList()
        };

        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client
            .PostAsJsonAsync("api/embed", payload, SerializerOptions, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaEmbedResponse>(SerializerOptions, ct)
            .ConfigureAwait(false);

        var embeddings = result?.Embeddings;
        if (embeddings is null || embeddings.Length == 0)
        {
            _logger.LogWarning("Ollama embed returned no embeddings for {Count} inputs", inputs.Count);
            return Array.Empty<float[]>();
        }

        if (embeddings.Length != inputs.Count)
        {
            _logger.LogWarning(
                "Ollama embed returned {Returned} embeddings for {Expected} inputs",
                embeddings.Length,
                inputs.Count);
        }

        return embeddings;
    }

    private sealed class OllamaEmbedRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<string> Input { get; set; } = new();
    }

    private sealed record OllamaEmbedResponse(float[][]? Embeddings);
}
