using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CoreAlign.Application.AiHelper;

public class AiHelperOptions
{
    public const string SectionName = "AiHelper";

    public bool Enabled { get; set; } = true;

    [Required]
    public string Provider { get; set; } = "Ollama";

    [Required]
    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string ChatBaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    [Required]
    public string ChatModel { get; set; } = "qwen2.5:7b";

    [Required]
    public string EmbeddingModel { get; set; } = "bge-m3";

    [Range(1, 8192)]
    public int EmbeddingDimensions { get; set; } = 1024;

    [Range(1, 50)]
    public int MaxContextChunks { get; set; } = 8;

    [Range(0.0, 1.0)]
    public double MinRelevanceScore { get; set; } = 0.35;

    [Range(0, 50)]
    public int MaxChunksPerSource { get; set; } = 3;

    [Range(0.0, 1.0)]
    public double DiversityLambda { get; set; } = 0.7;

    public Dictionary<string, double> SourceTypeWeights { get; set; } = new()
    {
        ["I18n"] = 0.6,
        ["Route"] = 1.0,
        ["ModuleDoc"] = 1.0,
        ["Article"] = 1.15,
        ["Sector"] = 1.1,
        ["SourceCode"] = 0.8,
    };

    [Range(0, 256)]
    public int NumThreads { get; set; }

    [Range(16, 8192)]
    public int MaxOutputTokens { get; set; } = 800;

    [Range(1, 10)]
    public int MaxToolIterations { get; set; } = 4;

    [Range(0, 20)]
    public int MaxHistoryTurns { get; set; } = 6;

    public string ApiKey { get; set; } = string.Empty;

    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.2;

    [Range(5, 600)]
    public int RequestTimeoutSeconds { get; set; } = 120;

    [Range(1, 1000)]
    public int PublicRateLimitPerMinute { get; set; } = 10;

    [Range(1, 1000)]
    public int AuthedRateLimitPerMinute { get; set; } = 30;

    public string ContentRoot { get; set; } = string.Empty;

    public bool IngestModuleDocs { get; set; } = true;

    public bool IngestSourceCode { get; set; }

    public string ModuleDocsRoot { get; set; } = string.Empty;

    public string SourceCodeRoot { get; set; } = string.Empty;

    public int MaxIngestFileBytes { get; set; } = 64000;

    public string[] SourceCodeExtensions { get; set; } = [".cs", ".ts", ".tsx"];

    public string[] SourceCodeExcludes { get; set; } =
        ["\\bin\\", "\\obj\\", "node_modules", "\\dist\\", "\\migrations\\", ".designer.cs", ".test.", ".spec."];
}
