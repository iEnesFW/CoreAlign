using CoreAlign.Application.Reports.DTOs;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Queries;

public record GetDuplicatesQuery(string Entity, DuplicateKeyKind Key)
    : IRequest<DuplicateReportDto>;
