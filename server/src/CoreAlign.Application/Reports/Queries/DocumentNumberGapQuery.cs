using CoreAlign.Application.Reports.DTOs;
using MediatR;

namespace CoreAlign.Application.Reports.Queries;

public record GetDocumentNumberGapsQuery(int? Year) : IRequest<DocumentNumberGapReportDto>;
