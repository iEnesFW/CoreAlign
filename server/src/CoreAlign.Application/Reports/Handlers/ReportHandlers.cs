using System.Globalization;
using CoreAlign.Application.Reports.DTOs;
using CoreAlign.Application.Reports.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Handlers;

public class GetSalesByPeriodQueryHandler : IRequestHandler<GetSalesByPeriodQuery, SalesByPeriodReportDto>
{
    private readonly IReportRepository _reports;

    public GetSalesByPeriodQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<SalesByPeriodReportDto> Handle(GetSalesByPeriodQuery request, CancellationToken ct)
    {
        var bucket = request.Bucket.ToString();
        var rows = await _reports.GetSalesByPeriodAsync(request.FromUtc, request.ToUtc, bucket, ct);

        var totalRevenue = rows.Sum(r => r.Revenue);
        var totalPaid = rows.Sum(r => r.Paid);
        var invoiceCount = rows.Sum(r => r.InvoiceCount);
        var customerCount = rows.Sum(r => r.CustomerCount);

        return new SalesByPeriodReportDto
        {
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Currency = "TRY",
            TotalRevenue = totalRevenue,
            TotalPaid = totalPaid,
            InvoiceCount = invoiceCount,
            CustomerCount = customerCount,
            Points = rows.Select(r => new SalesPeriodPointDto
            {
                PeriodKey = r.PeriodKey,
                Label = FormatLabel(r.BucketStart, bucket),
                BucketStart = r.BucketStart,
                Revenue = r.Revenue,
                Paid = r.Paid,
                InvoiceCount = r.InvoiceCount,
            }).ToList(),
        };
    }

    private static string FormatLabel(DateTime bucketStart, string bucket) =>
        bucket switch
        {
            "Day" => bucketStart.ToString("MMM d", CultureInfo.InvariantCulture),
            "Week" => $"W{ISOWeek.GetWeekOfYear(bucketStart):D2} {bucketStart:yyyy}",
            _ => bucketStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
        };
}

public class GetTopCustomersQueryHandler : IRequestHandler<GetTopCustomersQuery, List<TopCustomerReportDto>>
{
    private readonly IReportRepository _reports;

    public GetTopCustomersQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<List<TopCustomerReportDto>> Handle(GetTopCustomersQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);
        var rows = await _reports.GetTopCustomersAsync(limit, request.FromUtc, request.ToUtc, ct);
        return rows.Select(r => new TopCustomerReportDto
        {
            CustomerId = r.CustomerId,
            Name = r.Name,
            Code = r.Code,
            Currency = r.Currency,
            TotalRevenue = r.TotalRevenue,
            TotalPaid = r.TotalPaid,
            Outstanding = r.Outstanding,
            InvoiceCount = r.InvoiceCount,
            OrderCount = r.OrderCount,
            LastOrderAtUtc = r.LastOrderAt,
        }).ToList();
    }
}

public class GetTopProductsQueryHandler : IRequestHandler<GetTopProductsQuery, List<TopProductReportDto>>
{
    private readonly IReportRepository _reports;

    public GetTopProductsQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<List<TopProductReportDto>> Handle(GetTopProductsQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 100);
        var rows = await _reports.GetTopProductsGlobalAsync(limit, request.FromUtc, request.ToUtc, ct);
        return rows.Select(r => new TopProductReportDto
        {
            ProductId = r.ProductId,
            ProductSku = r.ProductSku,
            ProductName = r.ProductName,
            Quantity = r.Quantity,
            Revenue = r.Revenue,
            InvoiceCount = r.InvoiceCount,
        }).ToList();
    }
}

public class GetAgingSummaryQueryHandler : IRequestHandler<GetAgingSummaryQuery, AgingSummaryReportDto>
{
    private readonly IReportRepository _reports;

    public GetAgingSummaryQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<AgingSummaryReportDto> Handle(GetAgingSummaryQuery request, CancellationToken ct)
    {
        var asOf = request.AsOfUtc ?? DateTime.UtcNow;
        // Repo returns pre-bucketed aggregates per (customer, currency) — handler
        // no longer materializes every open invoice in memory.
        var buckets = await _reports.GetAgingBucketsAsync(asOf, ct);

        var grouped = buckets
            .Select(b => new CustomerAgingRowDto
            {
                CustomerId = b.CustomerId,
                CustomerName = b.CustomerName,
                Currency = b.Currency,
                Current = b.Current,
                Days1To30 = b.Days1To30,
                Days31To60 = b.Days31To60,
                Days61To90 = b.Days61To90,
                DaysOver90 = b.DaysOver90,
                TotalOutstanding = b.Current + b.Days1To30 + b.Days31To60 + b.Days61To90 + b.DaysOver90,
            })
            .Where(r => r.TotalOutstanding > 0)
            .OrderByDescending(r => r.TotalOutstanding)
            .ToList();

        var totals = grouped.Aggregate(
            new AgingSummaryReportDto { Currency = grouped.FirstOrDefault()?.Currency ?? "TRY" },
            (acc, r) =>
            {
                acc.Current += r.Current;
                acc.Days1To30 += r.Days1To30;
                acc.Days31To60 += r.Days31To60;
                acc.Days61To90 += r.Days61To90;
                acc.DaysOver90 += r.DaysOver90;
                acc.TotalOutstanding += r.TotalOutstanding;
                return acc;
            });
        totals.ByCustomer = grouped;
        totals.CustomersWithBalance = grouped.Count;
        return totals;
    }
}
