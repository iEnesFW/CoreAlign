using CoreAlign.Application.Common;
using CoreAlign.Application.Invoices.DTOs;
using CoreAlign.Application.Invoices.Handlers;
using CoreAlign.Application.Payments.DTOs;
using CoreAlign.Application.Payments.Mapping;
using CoreAlign.Application.Payments.Queries;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Payments.Handlers;

public class GetPaymentByIdHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    private readonly IPaymentRepository _payments;
    public GetPaymentByIdHandler(IPaymentRepository payments) => _payments = payments;

    public async Task<PaymentDto?> Handle(GetPaymentByIdQuery q, CancellationToken ct)
    {
        var p = await _payments.GetWithApplicationsAsync(q.Id, ct);
        return p is null ? null : PaymentMapper.ToDto(p);
    }
}

public class SearchPaymentsHandler : IRequestHandler<SearchPaymentsQuery, PagedResult<PaymentSummaryDto>>
{
    private readonly IPaymentRepository _payments;
    public SearchPaymentsHandler(IPaymentRepository payments) => _payments = payments;

    public async Task<PagedResult<PaymentSummaryDto>> Handle(SearchPaymentsQuery q, CancellationToken ct)
    {
        var (items, total) = await _payments.SearchAsync(q.Search, q.CustomerId, q.Page, q.PageSize, ct);
        return new PagedResult<PaymentSummaryDto>
        {
            Items = items.Select(PaymentMapper.ToSummaryDto).ToList(),
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }
}

public class GetPaymentsByCustomerHandler : IRequestHandler<GetPaymentsByCustomerQuery, IReadOnlyList<PaymentSummaryDto>>
{
    private readonly IPaymentRepository _payments;
    public GetPaymentsByCustomerHandler(IPaymentRepository payments) => _payments = payments;

    public async Task<IReadOnlyList<PaymentSummaryDto>> Handle(GetPaymentsByCustomerQuery q, CancellationToken ct) =>
        (await _payments.GetByCustomerAsync(q.CustomerId, ct)).Select(PaymentMapper.ToSummaryDto).ToList();
}

public class GetCustomerLedgerHandler : IRequestHandler<GetCustomerLedgerQuery, PagedResult<CustomerLedgerEntryDto>>
{
    private readonly ICustomerLedgerRepository _ledger;
    public GetCustomerLedgerHandler(ICustomerLedgerRepository ledger) => _ledger = ledger;

    public async Task<PagedResult<CustomerLedgerEntryDto>> Handle(GetCustomerLedgerQuery q, CancellationToken ct)
    {
        var (items, total) = await _ledger.SearchByCustomerAsync(q.CustomerId, q.FromUtc, q.ToUtc, q.Page, q.PageSize, ct);
        return new PagedResult<CustomerLedgerEntryDto>
        {
            Items = items.Select(PaymentMapper.ToDto).ToList(),
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }
}

public class GetCustomerAgingHandler : IRequestHandler<GetCustomerAgingQuery, CustomerAgingDto>
{
    private readonly IInvoiceRepository _invoices;
    public GetCustomerAgingHandler(IInvoiceRepository invoices) => _invoices = invoices;

    public async Task<CustomerAgingDto> Handle(GetCustomerAgingQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfUtc ?? DateTime.UtcNow;
        var open = await _invoices.GetOpenForCustomerAsync(q.CustomerId, ct);

        var currency = open.FirstOrDefault()?.Currency ?? "TRY";
        decimal current = 0m, b1 = 0m, b2 = 0m, b3 = 0m, b4 = 0m;

        foreach (var inv in open)
        {
            var remaining = inv.Total - inv.AmountPaid;
            if (remaining <= 0m) continue;
            var days = (asOf - inv.DueDate).TotalDays;
            if (days <= 0) current += remaining;
            else if (days <= 30) b1 += remaining;
            else if (days <= 60) b2 += remaining;
            else if (days <= 90) b3 += remaining;
            else b4 += remaining;
        }

        return new CustomerAgingDto
        {
            CustomerId = q.CustomerId,
            Currency = currency,
            Current = current,
            Days1To30 = b1,
            Days31To60 = b2,
            Days61To90 = b3,
            DaysOver90 = b4,
            TotalOutstanding = current + b1 + b2 + b3 + b4,
            Buckets = new List<AgingBucketDto>
            {
                new() { Bucket = "Current", Amount = current, InvoiceCount = open.Count(i => (asOf - i.DueDate).TotalDays <= 0) },
                new() { Bucket = "1-30", Amount = b1, InvoiceCount = open.Count(i => { var d = (asOf - i.DueDate).TotalDays; return d > 0 && d <= 30; }) },
                new() { Bucket = "31-60", Amount = b2, InvoiceCount = open.Count(i => { var d = (asOf - i.DueDate).TotalDays; return d > 30 && d <= 60; }) },
                new() { Bucket = "61-90", Amount = b3, InvoiceCount = open.Count(i => { var d = (asOf - i.DueDate).TotalDays; return d > 60 && d <= 90; }) },
                new() { Bucket = "90+", Amount = b4, InvoiceCount = open.Count(i => (asOf - i.DueDate).TotalDays > 90) },
            }
        };
    }
}

public class GetOpenInvoicesForCustomerHandler : IRequestHandler<GetOpenInvoicesForCustomerQuery, IReadOnlyList<InvoiceSummaryDto>>
{
    private readonly IInvoiceRepository _invoices;
    public GetOpenInvoicesForCustomerHandler(IInvoiceRepository invoices) => _invoices = invoices;

    public async Task<IReadOnlyList<InvoiceSummaryDto>> Handle(GetOpenInvoicesForCustomerQuery q, CancellationToken ct) =>
        (await _invoices.GetOpenForCustomerAsync(q.CustomerId, ct)).Select(InvoiceMapper.ToSummaryDto).ToList();
}
