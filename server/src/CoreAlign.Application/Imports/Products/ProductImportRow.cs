namespace CoreAlign.Application.Imports.Products;

public class ProductImportRow
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal Price { get; set; }
    public decimal ListPrice { get; set; }
    public decimal StandardCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal StockQuantity { get; set; }
    public decimal MinStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public bool IsStockTracked { get; set; } = true;
}
