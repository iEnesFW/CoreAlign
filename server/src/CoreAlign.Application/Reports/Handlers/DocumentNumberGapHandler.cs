using CoreAlign.Application.Reports.DTOs;
using CoreAlign.Application.Reports.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Handlers;

public class GetDocumentNumberGapsQueryHandler
    : IRequestHandler<GetDocumentNumberGapsQuery, DocumentNumberGapReportDto>
{
    private readonly IDocumentNumberGapReader _reader;
    private readonly ITenantContext _tenant;

    public GetDocumentNumberGapsQueryHandler(IDocumentNumberGapReader reader, ITenantContext tenant)
    {
        _reader = reader;
        _tenant = tenant;
    }

    public async Task<DocumentNumberGapReportDto> Handle(
        GetDocumentNumberGapsQuery request,
        CancellationToken ct)
    {
        if (!_tenant.HasTenant || _tenant.CurrentTenantId is null)
        {
            return new DocumentNumberGapReportDto { Year = request.Year };
        }

        var rows = await _reader.GetGapsAsync(_tenant.CurrentTenantId.Value, request.Year, ct);

        return new DocumentNumberGapReportDto
        {
            Year = request.Year,
            TypeCount = rows.Count,
            TotalGap = rows.Sum(r => r.GapCount),
            Rows = rows
                .Select(r => new DocumentNumberGapRowDto
                {
                    DocumentType = r.DocumentType,
                    Prefix = r.Prefix,
                    Year = r.Year,
                    Expected = r.Expected,
                    UsedCount = r.UsedCount,
                    MaxUsed = r.MaxUsed,
                    GapCount = r.GapCount,
                    MissingNumbers = r.MissingNumbers,
                })
                .ToList(),
        };
    }
}
