using CoreAlign.Application.Jobs;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreAlign.Application.Tests.Quotes;

public class QuoteExpiryJobTests
{
    private readonly IQuoteRepository _quotes = Substitute.For<IQuoteRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly QuoteExpiryJob _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public QuoteExpiryJobTests()
    {
        _sut = new QuoteExpiryJob(_quotes, _uow, NullLogger<QuoteExpiryJob>.Instance);
    }

    [Fact]
    public async Task Expires_all_quotes_returned_by_repository()
    {
        var q1 = BuildSentQuote("QUO-1", validInDays: -1);
        var q2 = BuildSentQuote("QUO-2", validInDays: -7);
        _quotes
            .GetExpirableSentQuotesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Quote> { q1, q2 });

        await _sut.RunAsync(CancellationToken.None);

        q1.Status.Should().Be(QuoteStatus.Expired);
        q2.Status.Should().Be(QuoteStatus.Expired);
        _quotes.Received(2).Update(Arg.Any<Quote>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task No_quotes_means_no_save()
    {
        _quotes
            .GetExpirableSentQuotesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Quote>());

        await _sut.RunAsync(CancellationToken.None);

        _quotes.DidNotReceive().Update(Arg.Any<Quote>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Quote BuildSentQuote(string number, int validInDays)
    {
        var quote = new Quote(
            number,
            CustomerId,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(validInDays),
            "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };
        var line = new QuoteLine(ProductId, "SKU", "Item", 1m, 100m) { TenantId = TenantId };
        line.ApplyPricing(1m, 100m, 100m, 0m, 0m, false, 0m, null, false, 0m, null, null, 1m, null, null);
        quote.ReplaceLines(new[] { line });
        quote.MarkSent();
        return quote;
    }
}
