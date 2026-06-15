using CoreAlign.Application.Accounting.Services;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.Accounting;

public class GLPostingServiceTests
{
    private readonly IJournalEntryRepository _journals = Substitute.For<IJournalEntryRepository>();
    private readonly IGLAccountRepository _accounts = Substitute.For<IGLAccountRepository>();
    private readonly IGLPostingMappingRepository _mappings = Substitute.For<IGLPostingMappingRepository>();
    private readonly IDocumentSequenceRepository _sequences = Substitute.For<IDocumentSequenceRepository>();
    private readonly IAccountingPeriodRepository _periods = Substitute.For<IAccountingPeriodRepository>();
    private readonly List<GLAccount> _chart = new();
    private readonly GLPostingService _sut;

    public GLPostingServiceTests()
    {
        _sequences.GetAsync(DocumentSequenceType.JournalNumber, Arg.Any<CancellationToken>())
            .Returns(new DocumentSequence(DocumentSequenceType.JournalNumber, "YEV", 2026, 1, 5));
        // The service batch-loads the whole chart + overrides once per posting.
        _accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(_chart);
        _mappings.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<GLPostingMapping>());
        _sut = new GLPostingService(_journals, _accounts, _mappings, _sequences, _periods);
    }

    private void MapAccount(string code) =>
        _chart.Add(new GLAccount(code, $"Account {code}", AccountType.Asset, isPostable: true));

    private static GLPostingRequest SalesRequest() => new(
        JournalSourceType.SalesInvoice,
        Guid.NewGuid(),
        "INV-1",
        DateTime.UtcNow.Date,
        JournalEntryType.Mahsup,
        "Satış faturası INV-1",
        new[]
        {
            new GLPostingLine(GLPostingKey.AccountsReceivable, 1180m, 0m),
            new GLPostingLine(GLPostingKey.SalesRevenue, 0m, 1000m),
            new GLPostingLine(GLPostingKey.OutputVat, 0m, 180m),
        });

    [Fact]
    public async Task Posts_a_balanced_posted_entry_when_all_accounts_resolve()
    {
        MapAccount("120");
        MapAccount("600");
        MapAccount("391");

        await _sut.PostAsync(SalesRequest(), default);

        await _journals.Received(1).AddAsync(
            Arg.Is<JournalEntry>(j =>
                j.Status == JournalEntryStatus.Posted &&
                j.TotalDebit == 1180m &&
                j.TotalCredit == 1180m &&
                j.SourceType == JournalSourceType.SalesInvoice &&
                j.Lines.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Skips_when_already_posted_for_the_source_document()
    {
        MapAccount("120");
        MapAccount("600");
        MapAccount("391");
        _journals.ExistsForSourceAsync(Arg.Any<JournalSourceType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.PostAsync(SalesRequest(), default);

        await _journals.DidNotReceive().AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Foreign_currency_posts_in_base_currency_at_rate_and_stays_balanced()
    {
        MapAccount("120");
        MapAccount("600");
        MapAccount("391");

        // USD invoice (1180 / 1000 / 180) at 30.00 → base amounts ×30, balanced.
        var request = SalesRequest() with { Currency = "USD", ExchangeRate = 30m };

        await _sut.PostAsync(request, default);

        await _journals.Received(1).AddAsync(
            Arg.Is<JournalEntry>(j =>
                j.Status == JournalEntryStatus.Posted &&
                j.TotalDebit == 35400m &&
                j.TotalCredit == 35400m &&
                j.Lines.All(l => l.Currency == "TRY" && l.ExchangeRate == 30m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Skips_entirely_when_any_account_is_unmapped()
    {
        // 391 (output VAT) intentionally left unmapped — a partial entry could
        // never balance, so nothing is posted.
        MapAccount("120");
        MapAccount("600");

        await _sut.PostAsync(SalesRequest(), default);

        await _journals.DidNotReceive().AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }
}
