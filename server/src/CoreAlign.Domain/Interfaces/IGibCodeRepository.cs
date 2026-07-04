using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IGibCodeRepository
{
    Task<IReadOnlyList<WithholdingTaxCode>> ListWithholdingCodesAsync(bool? isActive, CancellationToken ct = default);

    Task<IReadOnlyList<VatExemptionCode>> ListExemptionCodesAsync(bool? isActive, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, WithholdingTaxCode>> GetWithholdingByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<VatExemptionCode?> GetExemptionByIdAsync(Guid id, CancellationToken ct = default);
}
