using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace CoreAlign.Infrastructure.AiHelper;

public static class AiHelperMetrics
{
    public const string MeterName = "CoreAlign.AiHelper";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Requests =
        Meter.CreateCounter<long>("aihelper_requests_total", description: "AI Helper answers served.");

    private static readonly Counter<long> EmptyContext =
        Meter.CreateCounter<long>("aihelper_empty_context_total", description: "Answers where retrieval returned no relevant context.");

    private static readonly Histogram<double> TopScore =
        Meter.CreateHistogram<double>("aihelper_top_score", description: "Top retrieval cosine score per answer.");

    private static readonly Histogram<int> ContextChunks =
        Meter.CreateHistogram<int>("aihelper_context_chunks", description: "Number of context chunks fed to the model per answer.");

    private static readonly Counter<long> Feedback =
        Meter.CreateCounter<long>("aihelper_feedback_total", description: "User thumbs up/down feedback on answers.");

    public static void RecordRetrieval(string locale, bool isAnonymous, int chunkCount, double topScore)
    {
        var scope = isAnonymous ? "public" : "authed";
        var localeTag = new KeyValuePair<string, object?>("locale", locale);
        var scopeTag = new KeyValuePair<string, object?>("scope", scope);

        Requests.Add(1, localeTag, scopeTag);
        ContextChunks.Record(chunkCount, scopeTag);
        TopScore.Record(topScore, scopeTag);

        if (chunkCount == 0)
        {
            EmptyContext.Add(1, localeTag, scopeTag);
        }
    }

    public static void RecordFeedback(bool isHelpful) =>
        Feedback.Add(1, new KeyValuePair<string, object?>("helpful", isHelpful));
}
