using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Application.Documents;
using CoreAlign.Domain.Entities.Sales;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class DealerPortalCommissionHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly IDealerCommissionLedgerRepository _ledger = Substitute.For<IDealerCommissionLedgerRepository>();
    private readonly IDocumentService _documents = Substitute.For<IDocumentService>();

    public DealerPortalCommissionHandlerTests()
    {
        _scope.GetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(DealerAccountId);
    }

    [Fact]
    public async Task List_returns_filtered_and_mapped_entries()
    {
        var entry = BuildEntry(commissionAmount: 25m);
        _ledger.SearchAsync(DealerAccountId, null, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<DealerCommissionLedgerEntry>)new[] { entry }, 1));

        var handler = new ListDealerCommissionsHandler(_scope, _ledger);
        var result = await handler.Handle(new ListDealerCommissionsQuery(), default);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].CommissionAmount.Should().Be(25m);
    }

    [Fact]
    public async Task Summary_returns_repository_values()
    {
        _ledger.GetSummaryAsync(DealerAccountId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new DealerCommissionSummary(
                YtdAccrued: 500m, YtdPaid: 200m,
                ThisMonthAccrued: 100m, ThisMonthPaid: 50m,
                LifetimeAccrued: 1000m, LifetimePaid: 400m,
                Currency: "TRY"));

        var handler = new GetDealerCommissionSummaryHandler(_scope, _ledger);
        var result = await handler.Handle(new GetDealerCommissionSummaryQuery(), default);

        result.YtdAccrued.Should().Be(500m);
        result.ThisMonthAccrued.Should().Be(100m);
        result.LifetimePaid.Should().Be(400m);
        result.Currency.Should().Be("TRY");
    }

    [Fact]
    public async Task Statement_throws_when_date_range_is_inverted()
    {
        var handler = new DownloadDealerCommissionStatementHandler(_scope, _documents);
        var act = async () => await handler.Handle(
            new DownloadDealerCommissionStatementQuery(DateTime.UtcNow, DateTime.UtcNow.AddDays(-5)),
            default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Statement_delegates_to_document_service_with_resolved_dealer_id()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var doc = new DocumentResult(new byte[] { 1, 2, 3 }, "stmt.pdf");
        _documents.RenderDealerCommissionStatementPdfAsync(DealerAccountId, from, to, Arg.Any<CancellationToken>())
            .Returns(doc);

        var handler = new DownloadDealerCommissionStatementHandler(_scope, _documents);
        var result = await handler.Handle(new DownloadDealerCommissionStatementQuery(from, to), default);

        result.FileName.Should().Be("stmt.pdf");
        result.Content.Length.Should().Be(3);
    }

    private static DealerCommissionLedgerEntry BuildEntry(decimal commissionAmount)
    {
        var entry = new DealerCommissionLedgerEntry(
            dealerAccountId: DealerAccountId,
            orderId: Guid.NewGuid(),
            shipmentId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            currency: "TRY",
            orderTotal: 1000m,
            commissionPercent: commissionAmount / 10m,
            accruedAtUtc: DateTime.UtcNow,
            notes: null)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        return entry;
    }
}
