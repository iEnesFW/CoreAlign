using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;

namespace CoreAlign.Application.Tests.Quotes;

public class QuoteStateMachineTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Newly_created_quote_is_in_draft_state()
    {
        var quote = BuildQuote();
        quote.Status.Should().Be(QuoteStatus.Draft);
        quote.IsDraft.Should().BeTrue();
        quote.IsEditable.Should().BeTrue();
        quote.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void MarkSent_from_draft_with_lines_transitions_to_sent()
    {
        var quote = BuildQuoteWithLine();

        quote.MarkSent();

        quote.Status.Should().Be(QuoteStatus.Sent);
        quote.SentAtUtc.Should().NotBeNull();
        quote.IsEditable.Should().BeFalse();
    }

    [Fact]
    public void MarkSent_with_no_lines_throws()
    {
        var quote = BuildQuote();

        Action act = () => quote.MarkSent();

        act.Should().Throw<InvalidQuoteLineException>();
    }

    [Fact]
    public void MarkSent_from_non_draft_throws()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();

        Action act = () => quote.MarkSent();

        act.Should().Throw<InvalidQuoteStatusTransitionException>();
    }

    [Fact]
    public void Accept_from_sent_transitions_to_accepted()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();

        quote.Accept();

        quote.Status.Should().Be(QuoteStatus.Accepted);
        quote.AcceptedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Accept_from_draft_throws()
    {
        var quote = BuildQuoteWithLine();

        Action act = () => quote.Accept();

        act.Should().Throw<InvalidQuoteStatusTransitionException>();
    }

    [Fact]
    public void Reject_from_sent_transitions_to_rejected_and_stores_reason()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();

        quote.Reject("Too expensive");

        quote.Status.Should().Be(QuoteStatus.Rejected);
        quote.RejectionReason.Should().Be("Too expensive");
        quote.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Reject_from_accepted_throws()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();
        quote.Accept();

        Action act = () => quote.Reject("changed mind");

        act.Should().Throw<InvalidQuoteStatusTransitionException>();
    }

    [Fact]
    public void Expire_from_sent_transitions_to_expired()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();
        var now = DateTime.UtcNow;

        quote.Expire(now);

        quote.Status.Should().Be(QuoteStatus.Expired);
        quote.ExpiredAtUtc.Should().Be(now);
    }

    [Fact]
    public void Expire_from_accepted_throws()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();
        quote.Accept();

        Action act = () => quote.Expire(DateTime.UtcNow);

        act.Should().Throw<InvalidQuoteStatusTransitionException>();
    }

    [Fact]
    public void AttachConvertedOrder_after_accept_records_link()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();
        quote.Accept();
        var orderId = Guid.NewGuid();

        quote.AttachConvertedOrder(orderId);

        quote.ConvertedOrderId.Should().Be(orderId);
        quote.ConvertedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AttachConvertedOrder_twice_throws()
    {
        var quote = BuildQuoteWithLine();
        quote.MarkSent();
        quote.Accept();
        quote.AttachConvertedOrder(Guid.NewGuid());

        Action act = () => quote.AttachConvertedOrder(Guid.NewGuid());

        act.Should().Throw<QuoteAlreadyConvertedException>();
    }

    [Fact]
    public void Recalculate_aggregates_line_totals_correctly()
    {
        var quote = BuildQuote();
        var line1 = new QuoteLine(ProductId, "SKU1", "Item 1", 2m, 50m) { TenantId = TenantId };
        var line2 = new QuoteLine(Guid.NewGuid(), "SKU2", "Item 2", 3m, 20m) { TenantId = TenantId };
        line1.ApplyPricing(2m, 50m, 50m, 0m, 0m, false, 20m, null, false, 0m, null, null, 1m, null, null);
        line2.ApplyPricing(3m, 20m, 20m, 0m, 0m, false, 20m, null, false, 0m, null, null, 1m, null, null);

        quote.ReplaceLines(new[] { line1, line2 });

        quote.Subtotal.Should().Be(160m);
        quote.TaxTotal.Should().Be(32m);
        quote.Total.Should().Be(192m);
    }

    private static Quote BuildQuote(QuoteStatus? status = null)
    {
        var quote = new Quote(
            "QUO-TEST-0001",
            CustomerId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Customer = new Customer("Acme") { Id = CustomerId, TenantId = TenantId },
        };
        return quote;
    }

    private static Quote BuildQuoteWithLine()
    {
        var quote = BuildQuote();
        var line = new QuoteLine(ProductId, "SKU-A", "Widget", 5m, 10m)
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        line.ApplyPricing(5m, 10m, 10m, 0m, 0m, false, 0m, null, false, 0m, null, null, 1m, null, null);
        quote.ReplaceLines(new[] { line });
        return quote;
    }
}
