using CoreAlign.Application.Quotes.Commands;
using CoreAlign.Application.Quotes.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Quotes;

public class ConvertQuoteToOrderHandlerTests
{
    private readonly IQuoteRepository _quotes = Substitute.For<IQuoteRepository>();
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ConvertQuoteToOrderCommandHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    public ConvertQuoteToOrderHandlerTests()
    {
        _sequences
            .ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns("ORD-TEST-0001");
        _uow.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IUnitOfWorkTransaction>(new NoopTransaction()));
        _products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());
        _sut = new ConvertQuoteToOrderCommandHandler(_quotes, _orders, _customers, _sequences, _products, _uow);
    }

    [Fact]
    public async Task Converts_accepted_quote_into_draft_order_linked_via_source_quote_id()
    {
        var quote = BuildAcceptedQuote();
        _quotes.GetWithLinesAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(quote.Customer);

        Order? captured = null;
        await _orders.AddAsync(Arg.Do<Order>(o => captured = o), Arg.Any<CancellationToken>());

        var dto = await _sut.Handle(new ConvertQuoteToOrderCommand(quote.Id), default);

        dto.Should().NotBeNull();
        dto.Status.Should().Be(OrderStatus.Draft);
        captured.Should().NotBeNull();
        captured!.SourceQuoteId.Should().Be(quote.Id);
        captured.Lines.Should().HaveCount(1);
        captured.Lines.First().ProductId.Should().Be(ProductId);
        quote.ConvertedOrderId.Should().Be(captured.Id);
        quote.Status.Should().Be(QuoteStatus.Accepted);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(QuoteStatus.Draft)]
    [InlineData(QuoteStatus.Sent)]
    [InlineData(QuoteStatus.Rejected)]
    [InlineData(QuoteStatus.Expired)]
    public async Task Refuses_to_convert_when_quote_is_not_accepted(QuoteStatus status)
    {
        var quote = BuildQuoteInStatus(status);
        _quotes.GetWithLinesAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        Func<Task> act = () => _sut.Handle(new ConvertQuoteToOrderCommand(quote.Id), default);

        await act.Should().ThrowAsync<InvalidQuoteStatusTransitionException>();
        await _orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refuses_to_convert_twice()
    {
        var quote = BuildAcceptedQuote();
        quote.AttachConvertedOrder(Guid.NewGuid());
        _quotes.GetWithLinesAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        Func<Task> act = () => _sut.Handle(new ConvertQuoteToOrderCommand(quote.Id), default);

        await act.Should().ThrowAsync<QuoteAlreadyConvertedException>();
    }

    [Fact]
    public async Task Throws_when_quote_missing()
    {
        _quotes.GetWithLinesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Quote?)null);

        Func<Task> act = () => _sut.Handle(new ConvertQuoteToOrderCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<QuoteNotFoundException>();
    }

    [Fact]
    public async Task Concurrent_convert_attempts_only_create_one_order()
    {
        var quote = BuildAcceptedQuote();
        var serializingRepo = new SerializingQuoteRepository(quote);
        var orderRepo = Substitute.For<IOrderRepository>();
        var customerRepo = Substitute.For<ICustomerRepository>();
        var sequences = Substitute.For<IDocumentSequenceRepository>();
        var sequenceCounter = 0;
        sequences
            .ConsumeAsync(Arg.Any<DocumentSequenceType>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_ => $"ORD-CONC-{Interlocked.Increment(ref sequenceCounter):D4}");
        customerRepo.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(quote.Customer);

        var createdOrders = new List<Order>();
        var addOrderLock = new object();
        await orderRepo.AddAsync(
            Arg.Do<Order>(o =>
            {
                lock (addOrderLock)
                {
                    createdOrders.Add(o);
                }
            }),
            Arg.Any<CancellationToken>());

        var uow = Substitute.For<IUnitOfWork>();
        uow.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IUnitOfWorkTransaction>(new ReleasingTransaction(serializingRepo)));
        var products = Substitute.For<IProductRepository>();
        products.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Product>());
        var handler = new ConvertQuoteToOrderCommandHandler(
            serializingRepo, orderRepo, customerRepo, sequences, products, uow);

        var tasks = Enumerable
            .Range(0, 5)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await handler.Handle(new ConvertQuoteToOrderCommand(quote.Id), default);
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        createdOrders.Should().HaveCount(1);
        results.Count(r => r is null).Should().Be(1);
        results.Count(r => r is QuoteAlreadyConvertedException).Should().Be(4);
    }

    private sealed class SerializingQuoteRepository : IQuoteRepository
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly Quote _quote;
        private int _held;

        public SerializingQuoteRepository(Quote quote) => _quote = quote;

        public async Task AcquireConversionLockAsync(Guid quoteId, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            Interlocked.Exchange(ref _held, 1);
        }

        public void ReleaseConversionLock()
        {
            if (Interlocked.Exchange(ref _held, 0) == 1)
            {
                _lock.Release();
            }
        }

        public Task<Quote?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Quote?>(_quote);

        public Task<Quote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Quote?>(_quote);

        public Task<bool> QuoteNumberExistsAsync(string quoteNumber, Guid? excludeId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<(IReadOnlyList<QuoteSearchRow> Items, int Total)> SearchAsync(
            string? search,
            Guid? customerId,
            QuoteStatus? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult(((IReadOnlyList<QuoteSearchRow>)Array.Empty<QuoteSearchRow>(), 0));

        public Task<IReadOnlyList<Quote>> GetExpirableSentQuotesAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Quote>>(Array.Empty<Quote>());

        public Task AddAsync(Quote quote, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Update(Quote quote) { }

        public void Remove(Quote quote) { }
    }

    private sealed class NoopTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ReleasingTransaction : IUnitOfWorkTransaction
    {
        private readonly SerializingQuoteRepository _repo;
        public ReleasingTransaction(SerializingQuoteRepository repo) => _repo = repo;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync()
        {
            _repo.ReleaseConversionLock();
            return ValueTask.CompletedTask;
        }
    }

    private static Quote BuildAcceptedQuote() => BuildQuoteInStatus(QuoteStatus.Accepted);

    private static Quote BuildQuoteInStatus(QuoteStatus target)
    {
        var quote = new Quote(
            "QUO-CONV-0001",
            CustomerId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(15),
            "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };

        var line = new QuoteLine(ProductId, "SKU-A", "Widget", 4m, 25m) { TenantId = TenantId };
        line.ApplyPricing(4m, 25m, 25m, 0m, 0m, false, 20m, null, false, 0m, null, null, 1m, null, null);
        quote.ReplaceLines(new[] { line });

        if (target == QuoteStatus.Draft) return quote;

        quote.MarkSent();
        if (target == QuoteStatus.Sent) return quote;

        if (target == QuoteStatus.Accepted) { quote.Accept(); return quote; }
        if (target == QuoteStatus.Rejected) { quote.Reject("test"); return quote; }
        if (target == QuoteStatus.Expired) { quote.Expire(DateTime.UtcNow); return quote; }
        return quote;
    }
}
