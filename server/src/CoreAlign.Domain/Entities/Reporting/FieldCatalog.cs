namespace CoreAlign.Domain.Entities.Reporting;

public sealed record ReportFieldDescriptor(
    string Key,
    string LabelEn,
    string LabelTr,
    ReportFieldDataType DataType,
    bool IsDimension,
    bool IsMeasureEligible,
    IReadOnlyList<ReportFilterOperator> AllowedOperators,
    IReadOnlyList<ReportMeasureFunction>? AllowedAggregations = null);

public static class FieldCatalog
{
    private static readonly IReadOnlyDictionary<ReportEntityType, IReadOnlyList<ReportFieldDescriptor>> _catalog =
        Build();

    public static IReadOnlyList<ReportFieldDescriptor> For(ReportEntityType entityType) =>
        _catalog.TryGetValue(entityType, out var list) ? list : Array.Empty<ReportFieldDescriptor>();

    public static ReportFieldDescriptor? Find(ReportEntityType entityType, string key) =>
        For(entityType).FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

    public static bool IsKnown(ReportEntityType entityType, string key) =>
        Find(entityType, key) is not null;

    public static IReadOnlyList<ReportEntityType> SupportedEntities() => _catalog.Keys.ToList();

    private static IReadOnlyDictionary<ReportEntityType, IReadOnlyList<ReportFieldDescriptor>> Build()
    {
        var equalityOps = new[]
        {
            ReportFilterOperator.Equals,
            ReportFilterOperator.NotEquals,
            ReportFilterOperator.In,
        };
        var numericOps = new[]
        {
            ReportFilterOperator.Equals,
            ReportFilterOperator.NotEquals,
            ReportFilterOperator.GreaterThan,
            ReportFilterOperator.GreaterThanOrEqual,
            ReportFilterOperator.LessThan,
            ReportFilterOperator.LessThanOrEqual,
            ReportFilterOperator.Between,
        };
        var dateOps = new[]
        {
            ReportFilterOperator.Equals,
            ReportFilterOperator.GreaterThan,
            ReportFilterOperator.GreaterThanOrEqual,
            ReportFilterOperator.LessThan,
            ReportFilterOperator.LessThanOrEqual,
            ReportFilterOperator.Between,
        };
        var stringOps = new[]
        {
            ReportFilterOperator.Equals,
            ReportFilterOperator.NotEquals,
            ReportFilterOperator.Contains,
            ReportFilterOperator.StartsWith,
            ReportFilterOperator.In,
        };
        var sumAvgMinMax = new[]
        {
            ReportMeasureFunction.Sum,
            ReportMeasureFunction.Avg,
            ReportMeasureFunction.Min,
            ReportMeasureFunction.Max,
            ReportMeasureFunction.Count,
        };
        var countOnly = new[] { ReportMeasureFunction.Count };

        return new Dictionary<ReportEntityType, IReadOnlyList<ReportFieldDescriptor>>
        {
            [ReportEntityType.Invoice] = new ReportFieldDescriptor[]
            {
                new("InvoiceNumber", "Invoice number", "Fatura no", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("CustomerName", "Customer name", "Müşteri adı", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("Status", "Status", "Durum", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("Type", "Type", "Tip", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("Currency", "Currency", "Para birimi", ReportFieldDataType.String, true, false, equalityOps, countOnly),
                new("IssueDate", "Issue date", "Düzenleme tarihi", ReportFieldDataType.DateTime, true, false, dateOps, countOnly),
                new("DueDate", "Due date", "Vade tarihi", ReportFieldDataType.DateTime, true, false, dateOps, countOnly),
                new("Subtotal", "Subtotal", "Ara toplam", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("TaxTotal", "Tax total", "Vergi toplamı", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("Total", "Total", "Toplam", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("AmountPaid", "Amount paid", "Ödenen tutar", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
            },
            [ReportEntityType.Order] = new ReportFieldDescriptor[]
            {
                new("OrderNumber", "Order number", "Sipariş no", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("CustomerName", "Customer name", "Müşteri adı", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("Status", "Status", "Durum", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("Source", "Source", "Kaynak", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("Currency", "Currency", "Para birimi", ReportFieldDataType.String, true, false, equalityOps, countOnly),
                new("OrderDate", "Order date", "Sipariş tarihi", ReportFieldDataType.DateTime, true, false, dateOps, countOnly),
                new("Subtotal", "Subtotal", "Ara toplam", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("Total", "Total", "Toplam", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
            },
            [ReportEntityType.Customer] = new ReportFieldDescriptor[]
            {
                new("Code", "Code", "Kod", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("Name", "Name", "Ad", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("Type", "Type", "Tip", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("Status", "Status", "Durum", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("DefaultCurrency", "Currency", "Para birimi", ReportFieldDataType.String, true, false, equalityOps, countOnly),
                new("Territory", "Territory", "Bölge", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("CreatedAtUtc", "Created at", "Oluşturma tarihi", ReportFieldDataType.DateTime, true, false, dateOps, countOnly),
                new("CreditLimit", "Credit limit", "Kredi limiti", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("CurrentBalance", "Balance", "Bakiye", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("OverdueAmount", "Overdue", "Vadesi geçmiş", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
            },
            [ReportEntityType.Product] = new ReportFieldDescriptor[]
            {
                new("Sku", "SKU", "SKU", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("Name", "Name", "Ad", ReportFieldDataType.String, true, false, stringOps, countOnly),
                new("Currency", "Currency", "Para birimi", ReportFieldDataType.String, true, false, equalityOps, countOnly),
                new("Unit", "Unit", "Birim", ReportFieldDataType.String, true, false, equalityOps, countOnly),
                new("Price", "Price", "Fiyat", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("StandardCost", "Standard cost", "Standart maliyet", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("AverageCost", "Average cost", "Ortalama maliyet", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("StockQuantity", "Stock qty", "Stok miktarı", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
            },
            [ReportEntityType.StockMovement] = new ReportFieldDescriptor[]
            {
                new("Type", "Movement type", "Hareket tipi", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("SourceDocumentType", "Source", "Kaynak", ReportFieldDataType.Enum, true, false, equalityOps, countOnly),
                new("OccurredAtUtc", "Occurred at", "Hareket tarihi", ReportFieldDataType.DateTime, true, false, dateOps, countOnly),
                new("Quantity", "Quantity", "Miktar", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("UnitCost", "Unit cost", "Birim maliyet", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
                new("TotalCost", "Total cost", "Toplam maliyet", ReportFieldDataType.Decimal, false, true, numericOps, sumAvgMinMax),
            },
        };
    }
}
