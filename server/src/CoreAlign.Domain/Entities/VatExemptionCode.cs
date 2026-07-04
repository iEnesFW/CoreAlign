using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class VatExemptionCode : TenantEntity, IGlobalReadable
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LawReference { get; private set; }
    public VatExemptionKind Kind { get; private set; } = VatExemptionKind.Full;
    public bool IsActive { get; private set; } = true;

    protected VatExemptionCode() { }

    public VatExemptionCode(
        string code,
        string name,
        string? lawReference,
        VatExemptionKind kind,
        bool isActive = true)
    {
        Code = code;
        Name = name;
        LawReference = lawReference;
        Kind = kind;
        IsActive = isActive;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
