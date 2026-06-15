using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.Jobs;

public sealed class QuoteExpiryJob
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuoteExpiryJob> _logger;

    public QuoteExpiryJob(
        IQuoteRepository quoteRepository,
        IUnitOfWork unitOfWork,
        ILogger<QuoteExpiryJob> logger)
    {
        _quoteRepository = quoteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var quotes = await _quoteRepository.GetExpirableSentQuotesAsync(now, cancellationToken);

        if (quotes.Count == 0)
        {
            _logger.LogDebug("QuoteExpiryJob found no expirable Sent quotes at {NowUtc:o}.", now);
            return;
        }

        var expired = 0;
        foreach (var quote in quotes)
        {
            quote.Expire(now);
            _quoteRepository.Update(quote);
            expired++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("QuoteExpiryJob expired {Count} quote(s) at {NowUtc:o}.", expired, now);
    }
}
