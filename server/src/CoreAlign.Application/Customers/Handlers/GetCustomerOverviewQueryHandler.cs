using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Customers.Handlers;

public class GetCustomerOverviewQueryHandler : IRequestHandler<GetCustomerOverviewQuery, CustomerOverviewDto>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerAddressRepository _addresses;
    private readonly ICustomerContactRepository _contacts;
    private readonly ICustomerGroupRepository _groups;
    private readonly IUserRepository _users;
    private readonly IPriceListRepository _priceLists;
    private readonly IPaymentTermRepository _paymentTerms;
    private readonly IOrderRepository _orders;
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentRepository _payments;
    private readonly ICustomerLedgerRepository _ledger;

    public GetCustomerOverviewQueryHandler(
        ICustomerRepository customers,
        ICustomerAddressRepository addresses,
        ICustomerContactRepository contacts,
        ICustomerGroupRepository groups,
        IUserRepository users,
        IPriceListRepository priceLists,
        IPaymentTermRepository paymentTerms,
        IOrderRepository orders,
        IInvoiceRepository invoices,
        IPaymentRepository payments,
        ICustomerLedgerRepository ledger)
    {
        _customers = customers;
        _addresses = addresses;
        _contacts = contacts;
        _groups = groups;
        _users = users;
        _priceLists = priceLists;
        _paymentTerms = paymentTerms;
        _orders = orders;
        _invoices = invoices;
        _payments = payments;
        _ledger = ledger;
    }

    public async Task<CustomerOverviewDto> Handle(GetCustomerOverviewQuery request, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(request.Id, ct)
            ?? throw new CustomerNotFoundException();

        var groupTask = customer.CustomerGroupId.HasValue
            ? _groups.GetByIdAsync(customer.CustomerGroupId.Value, ct)
            : Task.FromResult<Domain.Entities.CustomerGroup?>(null);
        var repTask = customer.SalesRepUserId.HasValue
            ? _users.GetByIdAsync(customer.SalesRepUserId.Value, ct)
            : Task.FromResult<Domain.Entities.User?>(null);
        var priceListTask = customer.PriceListId.HasValue
            ? _priceLists.GetByIdAsync(customer.PriceListId.Value, ct)
            : Task.FromResult<Domain.Entities.PriceList?>(null);
        var termsTask = customer.PaymentTermsId.HasValue
            ? _paymentTerms.GetByIdAsync(customer.PaymentTermsId.Value, ct)
            : Task.FromResult<Domain.Entities.PaymentTerm?>(null);

        var addressesTask = _addresses.GetByCustomerAsync(customer.Id, ct);
        var contactsTask = _contacts.GetByCustomerAsync(customer.Id, ct);

        await Task.WhenAll(groupTask, repTask, priceListTask, termsTask, addressesTask, contactsTask);

        var addresses = await addressesTask;
        var primaryBilling = addresses.FirstOrDefault(a => a.IsPrimary) ?? addresses.FirstOrDefault();
        var primaryShipping = addresses.FirstOrDefault(a => a.IsPrimary && !ReferenceEquals(a, primaryBilling)) ?? primaryBilling;

        var contacts = await contactsTask;
        var primaryContact = contacts.FirstOrDefault(c => c.IsPrimary) ?? contacts.FirstOrDefault();

        var (_, _, _, outstanding, currency) = await _customers.GetInvoiceTotalsAsync(customer.Id, ct);

        var currentBalance = await _ledger.GetCurrentBalanceAsync(customer.Id, ct);
        if (currentBalance == 0m && customer.CurrentBalance != 0m)
        {
            currentBalance = customer.CurrentBalance;
        }

        var creditLimit = customer.CreditLimit;
        var creditAvailable = creditLimit > 0 ? Math.Max(0m, creditLimit - currentBalance) : 0m;
        var creditUsedPercent = creditLimit > 0 ? Math.Round((currentBalance / creditLimit) * 100m, 2) : 0m;

        var orders = await _orders.SearchAsync(null, customer.Id, 1, 5, ct);
        var invoices = await _invoices.SearchAsync(null, customer.Id, 1, 5, ct);
        var payments = await _payments.GetByCustomerAsync(customer.Id, ct);

        var lastOrderAt = orders.Items.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;
        var lastInvoiceAt = invoices.Items.OrderByDescending(i => i.IssueDate).FirstOrDefault()?.IssueDate;
        var lastPaymentAt = payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault()?.PaymentDate;

        var activity = new List<CustomerActivityItemDto>();
        foreach (var o in orders.Items.Take(5))
        {
            activity.Add(new CustomerActivityItemDto
            {
                OccurredAtUtc = o.OrderDate,
                Kind = "Order",
                SourceId = o.Id,
                SourceNumber = o.OrderNumber,
                Status = o.Status.ToString(),
                Amount = o.Total,
                Currency = o.Currency,
            });
        }
        foreach (var i in invoices.Items.Take(5))
        {
            activity.Add(new CustomerActivityItemDto
            {
                OccurredAtUtc = i.IssueDate,
                Kind = "Invoice",
                SourceId = i.Id,
                SourceNumber = i.InvoiceNumber,
                Status = i.Status.ToString(),
                Amount = i.Total,
                Currency = i.Currency,
            });
        }
        foreach (var p in payments.Take(5))
        {
            activity.Add(new CustomerActivityItemDto
            {
                OccurredAtUtc = p.PaymentDate,
                Kind = "Payment",
                SourceId = p.Id,
                SourceNumber = p.PaymentNumber,
                Status = p.Status.ToString(),
                Amount = p.Amount,
                Currency = p.Currency,
            });
        }

        return new CustomerOverviewDto
        {
            CustomerId = customer.Id,
            GroupName = (await groupTask)?.Name,
            SalesRepName = FormatUserName(await repTask),
            PriceListName = (await priceListTask)?.Name,
            PaymentTermsName = (await termsTask)?.Name,
            PaymentTermsNetDays = (await termsTask)?.NetDays,
            PrimaryBillingAddress = primaryBilling is null ? null : MapAddress(primaryBilling),
            PrimaryShippingAddress = primaryShipping is null ? null : MapAddress(primaryShipping),
            PrimaryContact = primaryContact is null ? null : MapContact(primaryContact),
            LastOrderAtUtc = lastOrderAt,
            LastInvoiceAtUtc = lastInvoiceAt,
            LastPaymentAtUtc = lastPaymentAt,
            CurrentBalance = currentBalance,
            Outstanding = outstanding,
            CreditLimit = creditLimit,
            CreditAvailable = creditAvailable,
            CreditUsedPercent = creditUsedPercent,
            IsOverCreditLimit = creditLimit > 0 && currentBalance > creditLimit,
            RecentActivity = activity
                .OrderByDescending(a => a.OccurredAtUtc)
                .Take(10)
                .ToList(),
        };
    }

    private static string? FormatUserName(Domain.Entities.User? user)
    {
        if (user is null) return null;
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? user.Username : name;
    }

    private static CustomerAddressDto MapAddress(Domain.Entities.CustomerAddress a) => new()
    {
        Id = a.Id,
        CustomerId = a.CustomerId,
        Label = a.Label,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
        IsPrimary = a.IsPrimary,
        CreatedAtUtc = a.CreatedAtUtc,
        UpdatedAtUtc = a.UpdatedAtUtc,
    };

    private static CustomerContactDto MapContact(Domain.Entities.CustomerContact c) => new()
    {
        Id = c.Id,
        CustomerId = c.CustomerId,
        Name = c.Name,
        Role = c.Role,
        Email = c.Email,
        Phone = c.Phone,
        Notes = c.Notes,
        IsPrimary = c.IsPrimary,
        CreatedAtUtc = c.CreatedAtUtc,
        UpdatedAtUtc = c.UpdatedAtUtc,
    };
}
