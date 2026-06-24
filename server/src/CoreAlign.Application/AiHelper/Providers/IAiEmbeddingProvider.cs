using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAlign.Application.AiHelper.Providers;

public interface IAiEmbeddingProvider
{
    string Name { get; }

    int Dimensions { get; }

    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken ct);
}
