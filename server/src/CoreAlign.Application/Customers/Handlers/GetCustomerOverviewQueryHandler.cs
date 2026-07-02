using CoreAlign.Application.Customers.DTOs;
using CoreAlign.Application.Customers.Queries;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Application.Customers.Handlers;

public class GetCustomerOverviewQueryHandler : IRequestHandler<GetCustomerOverviewQuery, CustomerOverviewDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IServiceScopeFactory _scopeFactory;

    public GetCustomerOverviewQueryHandler(
        ICustomerRepository customers,
        IServiceScopeFactory scopeFactory)
    {
        _customers = customers;
        _scopeFactory = scopeFactory;
    }

    public async Task<CustomerOverviewDto> Handle(GetCustomerOverviewQuery request, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(request.Id, ct)
            ?? throw new CustomerNotFoundException();
        var customerId = customer.Id;

        // Fan-out: 11 independent reads collapse to one wall-clock RTT. Each
        // task opens its own DI scope so DbContext thread-safety is preserved.
        var groupTask = customer.CustomerGroupId.HasValue
            ? RunScopedAsync<ICustomerGroupRepository, Domain.Entities.CustomerGroup?>(
                (r, t) => r.GetByIdAsync(customer.CustomerGroupId!.Value, t), ct)
            : Task.FromResult<Domain.Entities.CustomerGroup?>(null);
        var repTask = customer.SalesRepUserId.HasValue
            ? RunScopedAsync<IUserRepository, Domain.Entities.User?>(
                (r, t) => r.GetByIdAsync(customer.SalesRepUserId!.Value, t), ct)
            : Task.FromResult<Domain.Entities.User?>(null);
        var priceListTask = customer.PriceListId.HasValue
            ? RunScopedAsync<IPriceListRepository, Domain.Entities.PriceList?>(
                (r, t) => r.GetByIdAsync(customer.PriceListId!.Value, t), ct)
            : Task.FromResult<Domain.Entities.PriceList?>(null);
        var termsTask = customer.PaymentTermsId.HasValue
            ? RunScopedAsync<IPaymentTermRepository, Domain.Entities.PaymentTerm?>(
                (r, t) => r.GetByIdAsync(customer.PaymentTermsId!.Value, t), ct)
            : Task.FromResult<Domain.Entities.PaymentTerm?>(null);
        var addressesTask = RunScopedAsync<ICustomerAddressRepository, IReadOnlyList<Domain.Entities.CustomerAddress>>(
            (r, t) => r.GetByCustomerAsync(customerId, t), ct);
        var contactsTask = RunScopedAsync<ICustomerContactRepository, IReadOnlyList<Domain.Entities.CustomerContact>>(
            (r, t) => r.GetByCustomerAsync(customerId, t), ct);
        var invoiceTotalsTask = RunScopedAsync<ICustomerRepository, (int, decimal, decimal, decimal, string)>(
            (r, t) => r.GetInvoiceTotalsAsync(customerId, t), ct);
        var balanceTask = RunScopedAsync<ICustomerLedgerRepository, decimal>(
            (r, t) => r.GetCurrentBalanceAsync(customerId, t), ct);
        var ordersTask = RunScopedAsync<IOrderRepository, IReadOnlyList<OrderSearchRow>>(
            async (r, t) => (await r.SearchAsync(null, customerId, 1, 5, t)).Items, ct);
        var invoicesTask = RunScopedAsync<IInvoiceRepository, IReadOnlyList<InvoiceSearchRow>>(
            async (r, t) => (await r.SearchAsync(null, customerId, 1, 5, t)).Items, ct);
        var paymentsTask = RunScopedAsync<IPaymentRepository, IReadOnlyList<Domain.Entities.Payment>>(
            (r, t) => r.GetByCustomerAsync(customerId, 5, t), ct);

        await Task.WhenAll(
            groupTask, repTask, priceListTask, termsTask,
            addressesTask, contactsTask, invoiceTotalsTask, balanceTask,
            ordersTask, invoicesTask, paymentsTask);

        var group = groupTask.Result;
        var rep = repTask.Result;
        var priceList = priceListTask.Result;
        var terms = termsTask.Result;
        var addresses = addressesTask.Result;
        var contacts = contactsTask.Result;

        var primaryBilling = addresses.FirstOrDefault(a => a.IsPrimary) ?? addresses.FirstOrDefault();
        var primaryShipping = addresses.FirstOrDefault(a => a.IsPrimary && !ReferenceEquals(a, primaryBilling)) ?? primaryBilling;
        var primaryContact = contacts.FirstOrDefault(c => c.IsPrimary) ?? contacts.FirstOrDefault();

        var (_, _, _, outstanding, currency) = invoiceTotalsTask.Result;

        var currentBalance = balanceTask.Result;
        var creditLimit = customer.CreditLimit;
        var creditAvailable = creditLimit > 0 ? Math.Max(0m, creditLimit - currentBalance) : 0m;
        var creditUsedPercent = creditLimit > 0 ? Math.Round((currentBalance / creditLimit) * 100m, 2) : 0m;

        var orders = ordersTask.Result;
        var invoices = invoicesTask.Result;
        var payments = paymentsTask.Result;

        var lastOrderAt = orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;
        var lastInvoiceAt = invoices.OrderByDescending(i => i.IssueDate).FirstOrDefault()?.IssueDate;
        var lastPaymentAt = payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault()?.PaymentDate;

        var activity = new List<CustomerActivityItemDto>(15);
        foreach (var o in orders.Take(5))
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
        foreach (var i in invoices.Take(5))
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
            GroupName = group?.Name,
            SalesRepName = FormatUserName(rep),
            PriceListName = priceList?.Name,
            PaymentTermsName = terms?.Name,
            PaymentTermsNetDays = terms?.NetDays,
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

    private async Task<TResult> RunScopedAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> body,
        CancellationToken cancellationToken)
        where TService : notnull
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await body(service, cancellationToken);
    }
}
