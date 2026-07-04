using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PoNumberExistsAsync(string poNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<PurchaseOrder> Items, int Total)> SearchAsync(
        Guid? vendorId,
        PurchaseOrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);
    void Update(PurchaseOrder purchaseOrder);
    void Remove(PurchaseOrder purchaseOrder);
}

public interface IGoodsReceiptRepository
{
    Task<GoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GoodsReceipt?> GetByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<GoodsReceipt> Items, int Total)> SearchAsync(
        Guid? purchaseOrderId,
        Guid? vendorId,
        GoodsReceiptStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default);
    void Update(GoodsReceipt goodsReceipt);
}

public interface IVendorBillRepository
{
    Task<VendorBill?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorBill>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> BillNumberExistsAsync(Guid vendorId, string billNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<VendorBill> Items, int Total)> SearchAsync(
        Guid? vendorId,
        VendorBillStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(VendorBill bill, CancellationToken cancellationToken = default);
    void Update(VendorBill bill);

    /// <summary>
    /// Payables aging buckets per vendor, computed server-side from the unpaid
    /// portion of posted/partially-paid bills, bucketed by days-past-due relative
    /// to <paramref name="asOfUtc"/>.
    /// </summary>
    Task<IReadOnlyList<VendorAgingRow>> GetAgingBucketsAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
}

public record VendorAgingRow(
    Guid VendorId,
    string VendorName,
    string Currency,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal DaysOver90);

public interface IVendorPaymentRepository
{
    Task<VendorPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VendorPayment?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorPayment>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<VendorPayment> Items, int Total)> SearchAsync(
        Guid? vendorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(VendorPayment payment, CancellationToken cancellationToken = default);
    void Update(VendorPayment payment);
}

public interface IVendorPaymentApplicationRepository
{
    Task<VendorPaymentApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorPaymentApplication>> GetByVendorBillAsync(Guid vendorBillId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorPaymentApplication>> GetByVendorPaymentAsync(Guid vendorPaymentId, CancellationToken cancellationToken = default);
    Task<VendorPaymentApplication?> GetByPaymentAndBillAsync(Guid vendorPaymentId, Guid vendorBillId, CancellationToken cancellationToken = default);
    Task AddAsync(VendorPaymentApplication application, CancellationToken cancellationToken = default);
    void Remove(VendorPaymentApplication application);
}

public record ThreeWayMatchRow(
    Guid PurchaseOrderId,
    string PoNumber,
    Guid VendorId,
    string VendorName,
    string Currency,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal ExpectedQty,
    decimal ReceivedQty,
    decimal BilledQty,
    decimal ExpectedAmount,
    decimal BilledAmount,
    IReadOnlyList<string> Discrepancies);

public interface IThreeWayMatchReader
{
    Task<IReadOnlyList<ThreeWayMatchRow>> GetMismatchesAsync(
        Guid? vendorId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
}
