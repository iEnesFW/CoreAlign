using CoreAlign.Application.B2B;
using CoreAlign.Application.Billing;
using CoreAlign.Application.Billing.Payments;
using CoreAlign.Domain.Entities.Payments;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.CustomerPortal.Payments;

public class InitiateInvoicePaymentHandler : IRequestHandler<InitiateInvoicePaymentCommand, InitiateInvoicePaymentResult>
{
    private readonly IPortalScopeService _scope;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IInvoiceRepository _invoices;
    private readonly ICustomerRepository _customers;
    private readonly IPaymentSessionRepository _sessions;
    private readonly IPaymentGatewayRegistry _gateways;
    private readonly ITenantContext _tenant;
    private readonly IUnitOfWork _uow;
    private readonly IOptions<BillingOptions> _options;

    public InitiateInvoicePaymentHandler(
        IPortalScopeService scope,
        ICurrentUserAccessor currentUser,
        IInvoiceRepository invoices,
        ICustomerRepository customers,
        IPaymentSessionRepository sessions,
        IPaymentGatewayRegistry gateways,
        ITenantContext tenant,
        IUnitOfWork uow,
        IOptions<BillingOptions> options)
    {
        _scope = scope;
        _currentUser = currentUser;
        _invoices = invoices;
        _customers = customers;
        _sessions = sessions;
        _gateways = gateways;
        _tenant = tenant;
        _uow = uow;
        _options = options;
    }

    public async Task<InitiateInvoicePaymentResult> Handle(InitiateInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.InvoiceId == Guid.Empty) throw new ArgumentException("InvoiceId is required.", nameof(request));

        var customerId = await _scope.GetCurrentCustomerIdAsync(cancellationToken);
        var userId = _currentUser.UserIdOrThrow();
        var tenantId = _tenant.RequireTenantId();

        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new InvoiceNotFoundException();
        if (invoice.CustomerId != customerId) throw new InvoiceNotFoundException();
        if (!IsPayableStatus(invoice.Status)) throw new InvalidInvoiceStateException($"Invoice in status '{invoice.Status}' cannot be paid online.");

        var outstanding = Math.Round(Math.Max(0m, invoice.Total - invoice.AmountPaid), 4);
        if (outstanding <= 0m) throw new InvalidInvoiceStateException("Invoice has no outstanding amount.");

        var gatewayName = ResolveGatewayName(request.GatewayName);
        var gateway = _gateways.Find(gatewayName) ?? throw new PaymentGatewayNotConfiguredException(gatewayName);

        var customer = await _customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException();

        var billingInfo = BuildBillingInfo(request, customer);
        var lineItems = new[]
        {
            new PaymentLineItem(
                invoice.Id.ToString(),
                $"Invoice {invoice.InvoiceNumber}",
                "Invoice",
                outstanding),
        };

        var intentRequest = new PaymentIntentRequest(
            invoice.Id,
            invoice.InvoiceNumber,
            outstanding,
            invoice.Currency,
            tenantId,
            userId,
            $"Pay invoice {invoice.InvoiceNumber}",
            new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["invoiceId"] = invoice.Id.ToString(),
                ["invoiceNumber"] = invoice.InvoiceNumber,
                ["customerId"] = customer.Id.ToString(),
                ["sourceType"] = "Invoice",
            },
            billingInfo,
            lineItems);

        var intent = await gateway.CreateIntentAsync(intentRequest, cancellationToken);

        var session = new PaymentSession(
            invoice.Id,
            customer.Id,
            userId,
            gateway.Name,
            intent.IntentId,
            outstanding,
            invoice.Currency,
            intent.RedirectUrl);

        await _sessions.AddAsync(session, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new InitiateInvoicePaymentResult(
            session.Id,
            gateway.Name,
            intent.IntentId,
            intent.RedirectUrl,
            outstanding,
            invoice.Currency,
            invoice.InvoiceNumber);
    }

    private string ResolveGatewayName(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested)) return requested.Trim();
        var fallback = _options.Value.DefaultGatewayName;
        if (string.IsNullOrWhiteSpace(fallback)) throw new PaymentGatewayNotConfiguredException();
        return fallback.Trim();
    }

    private static bool IsPayableStatus(InvoiceStatus status) =>
        status is InvoiceStatus.Issued
            or InvoiceStatus.Sent
            or InvoiceStatus.PartiallyPaid
            or InvoiceStatus.Overdue;

    private static PaymentBillingInfo BuildBillingInfo(InitiateInvoicePaymentCommand request, Domain.Entities.Customer customer)
    {
        var ip = string.IsNullOrWhiteSpace(request.BuyerIpAddress) ? "127.0.0.1" : request.BuyerIpAddress!.Trim();
        if (request.BillingInfo is not null)
        {
            var bi = request.BillingInfo;
            return new PaymentBillingInfo(
                Name: SafeTrim(bi.Name),
                Surname: SafeTrim(bi.Surname),
                Email: SafeTrim(bi.Email),
                GsmNumber: SafeTrim(bi.GsmNumber),
                IdentityNumber: SafeTrim(bi.IdentityNumber),
                IpAddress: ip,
                Address: SafeTrim(bi.Address),
                City: SafeTrim(bi.City),
                Country: SafeTrim(bi.Country),
                ZipCode: SafeTrim(bi.ZipCode));
        }

        var name = SafeTrim(customer.TradeName ?? customer.Name);
        var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = parts.Length > 0 ? parts[0] : name;
        var lastName = parts.Length > 1 ? parts[1] : name;

        return new PaymentBillingInfo(
            Name: firstName,
            Surname: lastName,
            Email: SafeTrim(customer.Email),
            GsmNumber: SafeTrim(customer.Phone),
            IdentityNumber: SafeTrim(customer.TaxNumber),
            IpAddress: ip,
            Address: name,
            City: "Istanbul",
            Country: "TR",
            ZipCode: "00000");
    }

    private static string SafeTrim(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
