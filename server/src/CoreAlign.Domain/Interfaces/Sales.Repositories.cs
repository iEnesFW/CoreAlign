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
    Task<(IReadOnlyList<Order> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    void Update(Order order);
    void Remove(Order order);
}

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shipment?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Shipment> Items, int Total)> SearchAsync(
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

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Invoice> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetOpenForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    void Update(Invoice invoice);
}

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetWithApplicationsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Payment> Items, int Total)> SearchAsync(
        string? search,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    void Update(Payment payment);
}

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
