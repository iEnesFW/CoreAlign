using System.Globalization;
using CoreAlign.Application.Imports.Common;
using CoreAlign.Application.Products.Commands;
using FluentValidation;
using MediatR;

namespace CoreAlign.Application.Imports.Products;

public class ProductBulkImporter : BulkImporterBase<ProductImportRow>
{
    private static readonly string[] Headers =
    {
        "Sku","Name","Description","Barcode","Unit","Price","ListPrice","StandardCost","Currency","StockQuantity","MinStock","ReorderPoint","IsStockTracked"
    };

    private readonly IValidator<CreateProductCommand> _validator;
    private readonly IMediator _mediator;

    public ProductBulkImporter(
        IBulkImportRowReader reader,
        IBulkImportSessionStore sessions,
        IValidator<CreateProductCommand> validator,
        IMediator mediator)
        : base(reader, sessions)
    {
        _validator = validator;
        _mediator = mediator;
    }

    public override string EntityKind => "products";
    public override IReadOnlyList<string> ColumnHeaders => Headers;

    protected override ProductImportRow MapRaw(IReadOnlyDictionary<string, string> raw) => new()
    {
        Sku = raw.GetValueOrDefault("Sku") ?? string.Empty,
        Name = raw.GetValueOrDefault("Name") ?? string.Empty,
        Description = raw.GetValueOrDefault("Description"),
        Barcode = raw.GetValueOrDefault("Barcode"),
        Unit = string.IsNullOrWhiteSpace(raw.GetValueOrDefault("Unit")) ? "pcs" : raw["Unit"]!,
        Price = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("Price")),
        ListPrice = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("ListPrice")),
        StandardCost = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("StandardCost")),
        Currency = string.IsNullOrWhiteSpace(raw.GetValueOrDefault("Currency")) ? "TRY" : raw["Currency"]!,
        StockQuantity = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("StockQuantity")),
        MinStock = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("MinStock")),
        ReorderPoint = ParsingHelpers.ParseDecimal(raw.GetValueOrDefault("ReorderPoint")),
        IsStockTracked = ParsingHelpers.ParseBool(raw.GetValueOrDefault("IsStockTracked"), fallback: true)
    };

    protected override async Task<IReadOnlyList<BulkImportRowError>> ValidateRowAsync(
        ProductImportRow row,
        int rowNumber,
        CancellationToken cancellationToken)
    {
        var command = BuildCommand(row);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (validation.IsValid) return Array.Empty<BulkImportRowError>();
        return validation.Errors
            .Select(e => new BulkImportRowError
            {
                RowNumber = rowNumber,
                Field = e.PropertyName,
                Message = e.ErrorMessage
            })
            .ToList();
    }

    protected override async Task<bool> CommitRowAsync(ProductImportRow row, CancellationToken cancellationToken)
    {
        await _mediator.Send(BuildCommand(row), cancellationToken);
        return true;
    }

    private static CreateProductCommand BuildCommand(ProductImportRow row) => new(
        Sku: (row.Sku ?? string.Empty).Trim(),
        Name: (row.Name ?? string.Empty).Trim(),
        Description: NullIfEmpty(row.Description),
        Barcode: NullIfEmpty(row.Barcode),
        Unit: string.IsNullOrWhiteSpace(row.Unit) ? "pcs" : row.Unit.Trim(),
        Price: row.Price,
        ListPrice: row.ListPrice,
        StandardCost: row.StandardCost,
        Currency: string.IsNullOrWhiteSpace(row.Currency) ? "TRY" : row.Currency.ToUpper(CultureInfo.InvariantCulture),
        StockQuantity: row.StockQuantity,
        IsStockTracked: row.IsStockTracked,
        MinStock: row.MinStock,
        ReorderPoint: row.ReorderPoint);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
