namespace CoreAlign.Application.Reports.Common;

public sealed record GlDetailLineRow(
    DateTime PostingDate,
    string JournalNumber,
    string? Reference,
    string? Description,
    string? SourceDocumentNumber,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit);

public sealed record CashFlowRow(
    DateTime OccurredAtUtc,
    string Section,
    string Category,
    string Description,
    string Reference,
    decimal Amount,
    string Currency);

public sealed record PurchaseByVendorRow(
    Guid VendorId,
    string VendorName,
    string Currency,
    int PoCount,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total);

public sealed record PurchaseByProductRow(
    Guid ProductId,
    string Sku,
    string ProductName,
    string Currency,
    decimal QuantityOrdered,
    decimal Subtotal,
    decimal Total);

public sealed record OpenPoRow(
    Guid PurchaseOrderId,
    string PoNumber,
    DateTime OrderDate,
    DateTime? ExpectedDate,
    Guid VendorId,
    string VendorName,
    string Status,
    string Currency,
    decimal Total,
    int AgeDays);

public interface IReportDataReader
{
    Task<IReadOnlyList<GlDetailLineRow>> GetGlDetailAsync(
        Guid? accountId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CashFlowRow>> GetCashFlowAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseByVendorRow>> GetPurchaseByVendorAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseByProductRow>> GetPurchaseByProductAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenPoRow>> GetOpenPurchaseOrdersAsync(
        CancellationToken cancellationToken = default);
}
