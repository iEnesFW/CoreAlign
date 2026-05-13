using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities;

public class UnitOfMeasure : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Symbol { get; private set; }
    public Guid? BaseUomId { get; private set; }
    public decimal ConversionFactor { get; private set; } = 1m;
    public int DecimalPlaces { get; private set; } = 2;
    public bool IsActive { get; private set; } = true;

    public UnitOfMeasure? BaseUom { get; set; }

    protected UnitOfMeasure() { }

    public UnitOfMeasure(string code, string name, string? symbol = null, Guid? baseUomId = null, decimal conversionFactor = 1m, int decimalPlaces = 2)
    {
        if (conversionFactor <= 0m)
        {
            throw new ArgumentException("Conversion factor must be positive.", nameof(conversionFactor));
        }
        Code = code;
        Name = name;
        Symbol = symbol;
        BaseUomId = baseUomId;
        ConversionFactor = conversionFactor;
        DecimalPlaces = decimalPlaces;
    }

    public bool IsBase => BaseUomId is null;

    public void Update(string code, string name, string? symbol, Guid? baseUomId, decimal conversionFactor, int decimalPlaces, bool isActive)
    {
        if (conversionFactor <= 0m)
        {
            throw new ArgumentException("Conversion factor must be positive.", nameof(conversionFactor));
        }
        if (baseUomId == Id)
        {
            throw new ArgumentException("A UoM cannot reference itself as base.", nameof(baseUomId));
        }
        Code = code;
        Name = name;
        Symbol = symbol;
        BaseUomId = baseUomId;
        ConversionFactor = conversionFactor;
        DecimalPlaces = decimalPlaces;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
