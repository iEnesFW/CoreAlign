using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

public class WithholdingTaxCode : TenantEntity, IGlobalReadable
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public WithholdingKind Kind { get; private set; } = WithholdingKind.Partial;
    public int Numerator { get; private set; }
    public int Denominator { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public decimal Fraction => Denominator > 0 ? (decimal)Numerator / Denominator : 0m;

    protected WithholdingTaxCode() { }

    public WithholdingTaxCode(
        string code,
        string name,
        WithholdingKind kind,
        int numerator,
        int denominator,
        DateOnly validFrom,
        DateOnly? validTo = null,
        bool isActive = true)
    {
        Code = code;
        Name = name;
        Kind = kind;
        Numerator = numerator;
        Denominator = denominator;
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsActive = isActive;
    }

    public void UpdateRate(int numerator, int denominator, DateOnly validFrom)
    {
        Numerator = numerator;
        Denominator = denominator;
        ValidFrom = validFrom;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate(DateOnly validTo)
    {
        ValidTo = validTo;
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
