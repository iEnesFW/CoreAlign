using CoreAlign.Application.MasterData.DTOs;
using CoreAlign.Application.MasterData.Queries;
using CoreAlign.Domain.Interfaces;
using MediatR;

namespace CoreAlign.Application.MasterData.Handlers;

public class ListWithholdingTaxCodesHandler : IRequestHandler<ListWithholdingTaxCodesQuery, IReadOnlyList<WithholdingTaxCodeDto>>
{
    private readonly IGibCodeRepository _repo;

    public ListWithholdingTaxCodesHandler(IGibCodeRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<WithholdingTaxCodeDto>> Handle(ListWithholdingTaxCodesQuery q, CancellationToken ct)
        => (await _repo.ListWithholdingCodesAsync(q.IsActive, ct))
            .Select(x => new WithholdingTaxCodeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Kind = x.Kind.ToString(),
                Numerator = x.Numerator,
                Denominator = x.Denominator,
                IsActive = x.IsActive,
            })
            .ToList();
}

public class ListVatExemptionCodesHandler : IRequestHandler<ListVatExemptionCodesQuery, IReadOnlyList<VatExemptionCodeDto>>
{
    private readonly IGibCodeRepository _repo;

    public ListVatExemptionCodesHandler(IGibCodeRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<VatExemptionCodeDto>> Handle(ListVatExemptionCodesQuery q, CancellationToken ct)
        => (await _repo.ListExemptionCodesAsync(q.IsActive, ct))
            .Select(x => new VatExemptionCodeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                LawReference = x.LawReference,
                Kind = x.Kind.ToString(),
                IsActive = x.IsActive,
            })
            .ToList();
}
