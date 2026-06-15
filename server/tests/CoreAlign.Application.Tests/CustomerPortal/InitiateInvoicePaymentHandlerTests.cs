using CoreAlign.Application.B2B;
using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Application.CustomerPortal.Payments;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.CustomerPortal;

public class InitiateInvoicePaymentHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OtherCustomerId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IPortalScopeService _scope = Substitute.For<IPortalScopeService>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IPaymentSessionRepository _sessions = Substitute.For<IPaymentSessionRepository>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly InitiateInvoicePaymentHandler _sut;

    public InitiateInvoicePaymentHandlerTests()
    {
        _scope.GetCurrentCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(CustomerId);
        _currentUser.UserIdOrThrow().Returns(UserId);
        _tenant.RequireTenantId().Returns(TenantId);

        _gateway.Name.Returns("mock");
        _gateway.CreateIntentAsync(Arg.Any<PaymentIntentRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var req = call.Arg<PaymentIntentRequest>();
                return new PaymentIntentResult($"intent_{req.InvoiceIdOrFallback()}", "/redirect", PaymentIntentStatus.Pending, null, "{}");
            });

        var registry = new PaymentGatewayRegistry(new[] { _gateway });
        var options = Options.Create(new BillingOptions { DefaultGatewayName = "mock" });

        _sut = new InitiateInvoicePaymentHandler(_scope, _currentUser, _invoices, _customers, _sessions, registry, _tenant, _uow, options);
    }

    [Fact]
    public async Task Happy_path_creates_session_for_outstanding_amount()
    {
        var invoice = BuildIssuedInvoice(total: 1000m, amountPaid: 200m);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(BuildCustomer());

        PaymentSession? captured = null;
        await _sessions.AddAsync(Arg.Do<PaymentSession>(s => captured = s), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(new InitiateInvoicePaymentCommand(invoice.Id, null, null), default);

        result.Amount.Should().Be(800m);
        result.GatewayName.Should().Be("mock");
        result.RedirectUrl.Should().Be("/redirect");
        result.InvoiceNumber.Should().Be(invoice.InvoiceNumber);

        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(800m);
        captured.InvoiceId.Should().Be(invoice.Id);
        captured.CustomerId.Should().Be(CustomerId);
        captured.InitiatedByUserId.Should().Be(UserId);
        captured.Status.Should().Be(PaymentSessionStatus.Initiated);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pay_on_cross_customer_invoice_returns_not_found()
    {
        var invoice = BuildIssuedInvoice(total: 100m, amountPaid: 0m, customerId: OtherCustomerId);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var act = async () => await _sut.Handle(new InitiateInvoicePaymentCommand(invoice.Id, null, null), default);
        await act.Should().ThrowAsync<InvoiceNotFoundException>();

        await _sessions.DidNotReceive().AddAsync(Arg.Any<PaymentSession>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pay_on_draft_invoice_throws_invalid_state()
    {
        var invoice = new Invoice("INV-DRAFT", CustomerId, "Acme", "TRY") { Id = Guid.NewGuid(), TenantId = TenantId };
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var act = async () => await _sut.Handle(new InitiateInvoicePaymentCommand(invoice.Id, null, null), default);
        await act.Should().ThrowAsync<InvalidInvoiceStateException>();
    }

    [Fact]
    public async Task Pay_on_zero_outstanding_throws_invalid_state()
    {
        var invoice = BuildIssuedInvoice(total: 50m, amountPaid: 50m);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var act = async () => await _sut.Handle(new InitiateInvoicePaymentCommand(invoice.Id, null, null), default);
        await act.Should().ThrowAsync<InvalidInvoiceStateException>();
    }

    [Fact]
    public async Task Missing_invoice_throws_not_found()
    {
        var act = async () => await _sut.Handle(new InitiateInvoicePaymentCommand(Guid.NewGuid(), null, null), default);
        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    [Fact]
    public async Task Uses_explicit_gateway_name_when_supplied()
    {
        var invoice = BuildIssuedInvoice(total: 100m, amountPaid: 0m);
        _invoices.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        _customers.GetByIdAsync(CustomerId, Arg.Any<CancellationToken>()).Returns(BuildCustomer());

        var result = await _sut.Handle(new InitiateInvoicePaymentCommand(invoice.Id, null, "203.0.113.5", "mock"), default);

        result.GatewayName.Should().Be("mock");
        await _gateway.Received(1).CreateIntentAsync(
            Arg.Is<PaymentIntentRequest>(r => r.BillingInfo!.IpAddress == "203.0.113.5"),
            Arg.Any<CancellationToken>());
    }

    private static Invoice BuildIssuedInvoice(decimal total, decimal amountPaid, Guid? customerId = null)
    {
        var invoice = new Invoice($"INV-{Guid.NewGuid():N}".Substring(0, 12), customerId ?? CustomerId, "Acme Holding", "TRY")
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
        };
        invoice.Lines.Add(new InvoiceLine("SKU-FIX", "Fixture", null, quantity: 1m, unitPrice: total));
        invoice.Recalculate();
        invoice.Issue(invoice.InvoiceNumber);
        if (amountPaid > 0m)
        {
            invoice.RecordPayment(amountPaid, DateTime.UtcNow);
        }
        return invoice;
    }

    private static Customer BuildCustomer()
    {
        var customer = new Customer("Acme Holding", CustomerType.Business, code: "CUST-1", email: "billing@acme.test", phone: "+905000000000")
        {
            Id = CustomerId,
            TenantId = TenantId,
        };
        return customer;
    }
}

internal static class PaymentIntentRequestExtensions
{
    public static string InvoiceIdOrFallback(this PaymentIntentRequest request) =>
        request.OrderId == Guid.Empty ? Guid.NewGuid().ToString("N") : request.OrderId.ToString("N");
}
