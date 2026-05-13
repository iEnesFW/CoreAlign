using CoreAlign.Application.Common;
using CoreAlign.Application.Dashboard.DTOs;
using MediatR;

namespace CoreAlign.Application.Dashboard.Queries;

public record GetDashboardStatsQuery() : IRequest<DashboardStatsDto>;
