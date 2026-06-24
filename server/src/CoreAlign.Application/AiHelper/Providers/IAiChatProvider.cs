using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAlign.Application.AiHelper.Providers;

public interface IAiChatProvider
{
    string Name { get; }

    bool SupportsTools { get; }

    IAsyncEnumerable<AiChatDelta> StreamAsync(AiChatRequest request, CancellationToken ct);

    Task<AiChatCompletion> CompleteAsync(AiChatRequest request, CancellationToken ct);
}
