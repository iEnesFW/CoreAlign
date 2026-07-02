using CoreAlign.Application.Reports.DTOs;
using CoreAlign.Application.Reports.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.Reports.Handlers;

public class GetDuplicatesQueryHandler : IRequestHandler<GetDuplicatesQuery, DuplicateReportDto>
{
    private readonly ICustomerRepository _customers;
    private readonly IVendorRepository _vendors;

    public GetDuplicatesQueryHandler(ICustomerRepository customers, IVendorRepository vendors)
    {
        _customers = customers;
        _vendors = vendors;
    }

    public async Task<DuplicateReportDto> Handle(GetDuplicatesQuery request, CancellationToken ct)
    {
        var entity = string.Equals(request.Entity, "vendor", StringComparison.OrdinalIgnoreCase)
            ? "vendor"
            : "customer";

        var groups = entity == "vendor"
            ? await _vendors.FindDuplicatesAsync(request.Key, ct)
            : await _customers.FindDuplicatesAsync(request.Key, ct);

        return new DuplicateReportDto
        {
            Entity = entity,
            Key = request.Key.ToString(),
            GroupCount = groups.Count,
            Groups = groups
                .OrderByDescending(g => g.Count)
                .Select(g => new DuplicateGroupDto
                {
                    KeyValue = g.KeyValue,
                    Count = g.Count,
                    Members = g.Members
                        .Select(m => new DuplicateMemberDto { Id = m.Id, Name = m.Name })
                        .ToList(),
                })
                .ToList(),
        };
    }
}
