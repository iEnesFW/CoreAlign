using CoreAlign.Application.B2B;
using CoreAlign.Application.B2B.DealerPortal;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.B2B;

public class DealerPortalInvoicesHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid DealerAccountId = Guid.NewGuid();
    private static readonly Guid AllowedCustomerId = Guid.NewGuid();
    private static readonly Guid OtherCustomerId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();

    public DealerPortalInvoicesHandlerTests()
    {
        _scope.GetCurrentDealerAccountIdAsync(Arg.Any<CancellationToken>()).Returns(DealerAccountId);
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { AllowedCustomerId });
    }

    [Fact]
    public async Task ListInvoices_returns_empty_when_dealer_has_no_managed_customers()
    {
        _scope.GetDealerAllowedCustomerIdsAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Guid>());
        var handler = new ListDealerPortalInvoicesHandler(_scope, _invoices);

        var result = await handler.Handle(new ListDealerPortalInvoicesQuery(), default);

        result.Total.Should().Be(0);
        result.Items.Should().BeEmpty();
        await _invoices.DidNotReceive().SearchForCustomersAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<Domain.Enums.InvoiceStatus?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListInvoices_throws_when_filter_customerId_is_not_in_managed_customers()
    {
        var handler = new ListDealerPortalInvoicesHandler(_scope, _invoices);
        var act = async () => await handler.Handle(new ListDealerPortalInvoicesQuery(CustomerId: OtherCustomerId), default);

        await act.Should().ThrowAsync<DealerCustomerNotAuthorizedException>();
    }

    [Fact]
    public async Task ListInvoices_returns_paged_invoices_for_managed_customers()
    {
        var row = new InvoiceSearchRow(
            Guid.NewGuid(), "INV-1", Domain.Enums.InvoiceType.SalesInvoice, null,
            "Acme", DateTime.UtcNow, DateTime.UtcNow.AddDays(30),
            Domain.Enums.InvoiceStatus.Issued, "TRY", 100m, 0m);
        _invoices.SearchForCustomersAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(AllowedCustomerId)),
                null,
                null,
                null,
                1,
                20,
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<InvoiceSearchRow>)new[] { row }, 1));

        var handler = new ListDealerPortalInvoicesHandler(_scope, _invoices);
        var result = await handler.Handle(new ListDealerPortalInvoicesQuery(), default);

        result.Items.Should().HaveCount(1);
        result.Items[0].InvoiceNumber.Should().Be("INV-1");
    }

    [Fact]
    public async Task ListInvoices_pushes_status_and_date_filters_into_repository()
    {
        var statusValue = Domain.Enums.InvoiceStatus.Paid;
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        _invoices.SearchForCustomersAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                statusValue,
                from,
                to,
                1,
                20,
                Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<InvoiceSearchRow>)Array.Empty<InvoiceSearchRow>(), 0));

        var handler = new ListDealerPortalInvoicesHandler(_scope, _invoices);
        await handler.Handle(new ListDealerPortalInvoicesQuery(
            Status: statusValue.ToString(),
            FromUtc: from,
            ToUtc: to), default);

        await _invoices.Received(1).SearchForCustomersAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            statusValue,
            from,
            to,
            1,
            20,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInvoiceById_returns_404_when_invoice_is_for_a_customer_outside_dealer_scope()
    {
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice("INV-X", OtherCustomerId, "Acme", "TRY")
        {
            Id = invoiceId,
            TenantId = TenantId,
        };
        _invoices.GetWithLinesAsync(invoiceId, Arg.Any<CancellationToken>()).Returns(invoice);

        var handler = new GetDealerPortalInvoiceByIdHandler(_scope, _invoices);
        var act = async () => await handler.Handle(new GetDealerPortalInvoiceByIdQuery(invoiceId), default);

        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    [Fact]
    public async Task GetInvoiceById_returns_invoice_when_customer_is_in_dealer_scope()
    {
        var invoiceId = Guid.NewGuid();
        var invoice = new Invoice("INV-OK", AllowedCustomerId, "Acme", "TRY")
        {
            Id = invoiceId,
            TenantId = TenantId,
        };
        _invoices.GetWithLinesAsync(invoiceId, Arg.Any<CancellationToken>()).Returns(invoice);

        var handler = new GetDealerPortalInvoiceByIdHandler(_scope, _invoices);
        var result = await handler.Handle(new GetDealerPortalInvoiceByIdQuery(invoiceId), default);

        result.InvoiceNumber.Should().Be("INV-OK");
        result.CustomerId.Should().Be(AllowedCustomerId);
    }
}
