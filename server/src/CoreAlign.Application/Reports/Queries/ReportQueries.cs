using CoreAlign.Application.Reports.DTOs;
using MediatR;

namespace CoreAlign.Application.Reports.Queries;

public enum SalesBucket
{
    Day,
    Week,
    Month,
}

public record GetSalesByPeriodQuery(DateTime FromUtc, DateTime ToUtc, SalesBucket Bucket = SalesBucket.Month)
    : IRequest<SalesByPeriodReportDto>;

public record GetTopCustomersQuery(int Limit = 10, DateTime? FromUtc = null, DateTime? ToUtc = null)
    : IRequest<List<TopCustomerReportDto>>;

public record GetTopProductsQuery(int Limit = 10, DateTime? FromUtc = null, DateTime? ToUtc = null)
    : IRequest<List<TopProductReportDto>>;

public record GetAgingSummaryQuery(DateTime? AsOfUtc = null) : IRequest<AgingSummaryReportDto>;
