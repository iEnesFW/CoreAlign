using Microsoft.Extensions.Logging;

namespace CoreAlign.Infrastructure.Services;

public sealed class TcmbRetryHandler : DelegatingHandler
{
    private static readonly TimeSpan[] BackoffDelays =
    {
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(800),
        TimeSpan.FromMilliseconds(2400),
    };

    private readonly ILogger<TcmbRetryHandler> _logger;

    public TcmbRetryHandler(ILogger<TcmbRetryHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < BackoffDelays.Length; attempt++)
        {
            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode || (int)response.StatusCode < 500)
                {
                    return response;
                }
                response.Dispose();
                _logger.LogWarning("TCMB request to {Uri} failed with {Status} on attempt {Attempt}.", request.RequestUri, (int)response.StatusCode, attempt + 1);
            }
            catch (HttpRequestException ex) when (attempt < BackoffDelays.Length - 1)
            {
                _logger.LogWarning(ex, "TCMB request to {Uri} threw on attempt {Attempt}.", request.RequestUri, attempt + 1);
            }
            await Task.Delay(BackoffDelays[attempt], cancellationToken);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
