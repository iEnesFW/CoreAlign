using CoreAlign.Application.Reports.DTOs;
using MediatR;

namespace CoreAlign.Application.Reports.Queries;

public record GetCashPositionQuery(DateTime? AsOfUtc = null) : IRequest<CashPositionReportDto>;
