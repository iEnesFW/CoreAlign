using CoreAlign.Domain.Entities;

namespace CoreAlign.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int Total)> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Update(Product product);
    void Remove(Product product);
}

public interface IProductComponentRepository
{
    Task<ProductComponent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductComponent>> GetByParentAsync(Guid parentProductId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid parentProductId, Guid componentProductId, CancellationToken cancellationToken = default);
    Task<bool> WouldCreateCycleAsync(Guid parentProductId, Guid componentProductId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<(Guid ComponentId, decimal Quantity)>>> GetTreeForProductsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);
    Task AddAsync(ProductComponent component, CancellationToken cancellationToken = default);
    void Update(ProductComponent component);
    void Remove(ProductComponent component);
}

public interface IStockTransactionRepository
{
    Task AddAsync(StockTransaction transaction, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StockTransaction> Items, int Total)> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetWithLinesAndShipmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> OrderNumberExistsAsync(string orderNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    /// <summary>Slim list projection — see <see cref="OrderSearchRow"/>.</summary>
    Task<(IReadOnlyList<OrderSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatusGroup>> GetOrderStatusBreakdownAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task<(int OrderCount, decimal OrderTotal, DateTime? FirstOrderAt, DateTime? LastOrderAt)>
        GetOrderTotalsExtendedAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    void Update(Order order);
    void Remove(Order order);
}

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shipment?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    /// <summary>Slim list projection — see <see cref="ShipmentSearchRow"/>.</summary>
    Task<(IReadOnlyList<ShipmentSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        Guid? orderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);
    void Update(Shipment shipment);
    void Remove(Shipment shipment);
}

public record ShipmentSearchRow(
    Guid Id,
    string ShipmentNumber,
    Guid OrderId,
    Guid CustomerId,
    Guid WarehouseId,
    string? WarehouseName,
    Domain.Enums.ShipmentStatus Status,
    DateTime CreatedDate,
    DateTime? PickedAtUtc,
    DateTime? PackedAtUtc,
    DateTime? DispatchedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? CancelledAtUtc,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl,
    decimal? ShippingCost,
    string? ReceivedBy,
    string? Notes,
    string? CancelReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    /// <summary>
    /// List-view search that returns only the columns the list UI consumes —
    /// skips loading wide JSONB snapshot columns (CustomerSnapshot,
    /// BillingAddressSnapshot, ShippingAddressSnapshot, TaxBreakdownJson) and
    /// the long Notes/Terms text columns that would otherwise be hydrated per
    /// row on every page request.
    /// </summary>
    Task<(IReadOnlyList<InvoiceSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetOpenForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MonthlyInvoiceTotal>> GetMonthlyRevenueByCustomerAsync(
        Guid customerId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductLine>> GetTopProductsByCustomerAsync(
        Guid customerId,
        int limit,
        CancellationToken cancellationToken = default);
    Task<PaymentBehavior> GetPaymentBehaviorByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatusGroup>> GetInvoiceStatusBreakdownAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetCreditNotesForInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-load invoices by id in a single round-trip — replaces N×<see cref="GetByIdAsync"/>
    /// loops in handlers that apply/void payments touching multiple invoices at once.
    /// Returns a dictionary keyed by id; missing ids are simply absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Invoice>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    void Update(Invoice invoice);
}

public record PaymentSearchRow(
    Guid Id,
    string PaymentNumber,
    Domain.Enums.PaymentDirection Direction,
    Domain.Enums.PaymentStatus Status,
    Guid CustomerId,
    string CustomerName,
    DateTime PaymentDate,
    Domain.Enums.PaymentMethod Method,
    decimal Amount,
    decimal AppliedAmount,
    string Currency);

public record OrderSearchRow(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    DateTime OrderDate,
    Domain.Enums.OrderStatus Status,
    string Currency,
    decimal Total);

public record InvoiceSearchRow(
    Guid Id,
    string InvoiceNumber,
    Domain.Enums.InvoiceType Type,
    Guid? OrderId,
    string CustomerName,
    DateTime IssueDate,
    DateTime DueDate,
    Domain.Enums.InvoiceStatus Status,
    string Currency,
    decimal Total,
    decimal AmountPaid);

public record MonthlyInvoiceTotal(int Year, int Month, decimal Revenue, int InvoiceCount, decimal Paid);

public record TopProductLine(Guid? ProductId, string ProductSku, string ProductName, decimal Quantity, decimal Revenue, int InvoiceCount);

public record PaymentBehavior(int OnTimePaidCount, int LatePaidCount, double AvgDaysToPayment);

public record StatusGroup(string Status, int Count, decimal Total);

public interface IReportRepository
{
    Task<IReadOnlyList<SalesPeriodRow>> GetSalesByPeriodAsync(
        DateTime fromUtc,
        DateTime toUtc,
        string bucket,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopCustomerRow>> GetTopCustomersAsync(
        int limit,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductRow>> GetTopProductsGlobalAsync(
        int limit,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpenInvoiceRow>> GetOpenInvoicesAcrossCustomersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aging buckets computed server-side via conditional sums — replaces the
    /// "load every open invoice, bucket in memory" pattern in
    /// <c>GetAgingSummaryQueryHandler</c>. The bucket boundaries are expressed
    /// in days-past-due relative to <paramref name="asOfUtc"/>.
    /// </summary>
    Task<IReadOnlyList<AgingBucketRow>> GetAgingBucketsAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default);
}

public record AgingBucketRow(
    Guid CustomerId,
    string CustomerName,
    string Currency,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal DaysOver90);

public record SalesPeriodRow(string PeriodKey, DateTime BucketStart, decimal Revenue, decimal Paid, int InvoiceCount, int CustomerCount);

public record TopCustomerRow(
    Guid CustomerId,
    string Name,
    string? Code,
    string Currency,
    decimal TotalRevenue,
    decimal TotalPaid,
    decimal Outstanding,
    int InvoiceCount,
    int OrderCount,
    DateTime? LastOrderAt);

public record TopProductRow(Guid? ProductId, string ProductSku, string ProductName, decimal Quantity, decimal Revenue, int InvoiceCount);

public record OpenInvoiceRow(Guid CustomerId, string CustomerName, string Currency, decimal Outstanding, DateTime DueDate);

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetWithApplicationsAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Slim list projection — see <see cref="PaymentSearchRow"/>.</summary>
    Task<(IReadOnlyList<PaymentSearchRow> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Recent payments for a customer, hard-capped to <paramref name="limit"/> rows (default 50)
    /// to keep responses bounded even for high-volume customers.
    /// </summary>
    Task<IReadOnlyList<Payment>> GetByCustomerAsync(
        Guid customerId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregate counts/dates without materializing every row — used by the customer
    /// overview/analytics handlers in place of loading the full payment list.
    /// </summary>
    Task<PaymentSummaryAggregate> GetCustomerPaymentSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentApplication>> GetApplicationsByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    void Update(Payment payment);
}

public record PaymentSummaryAggregate(int Count, DateTime? LastPaymentAt, decimal TotalAmount);

public interface ICustomerLedgerRepository
{
    Task AddAsync(CustomerLedgerEntry entry, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CustomerLedgerEntry> Items, int Total)> SearchByCustomerAsync(
        Guid customerId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<decimal> GetCurrentBalanceAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<decimal> GetLastRunningBalanceAsync(Guid customerId, CancellationToken cancellationToken = default);
}
