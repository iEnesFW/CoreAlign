using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Interfaces;

public interface IDocumentSequenceRepository
{
    Task<DocumentSequence?> GetAsync(DocumentSequenceType type, CancellationToken cancellationToken = default);
    Task<string> ConsumeAsync(DocumentSequenceType type, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<string> PeekAsync(DocumentSequenceType type, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task EnsureExistsAsync(DocumentSequenceType type, string prefix, int padLength, int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentSequence>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(DocumentSequence sequence, CancellationToken cancellationToken = default);
    void Update(DocumentSequence sequence);
}

public interface ITaxRateRepository
{
    Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxRate?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxRate>> ListAsync(bool? isActive = null, bool? isWithholding = null, CancellationToken cancellationToken = default);
    Task AddAsync(TaxRate taxRate, CancellationToken cancellationToken = default);
    void Update(TaxRate taxRate);
    void Remove(TaxRate taxRate);
}

public interface IPaymentTermRepository
{
    Task<PaymentTerm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentTerm>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentTerm term, CancellationToken cancellationToken = default);
    void Update(PaymentTerm term);
    void Remove(PaymentTerm term);
}

public interface IPriceListRepository
{
    Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PriceList?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceList>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<PriceListItem?> GetItemAsync(Guid priceListId, Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(PriceList list, CancellationToken cancellationToken = default);
    Task AddItemAsync(PriceListItem item, CancellationToken cancellationToken = default);
    void Update(PriceList list);
    void UpdateItem(PriceListItem item);
    void Remove(PriceList list);
    void RemoveItem(PriceListItem item);
}

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Brand>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);
    void Update(Brand brand);
    void Remove(Brand brand);
}

public interface IProductCategoryRepository
{
    Task<ProductCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCategory>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCategory>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default);
    void Update(ProductCategory category);
    void Remove(ProductCategory category);
}

public interface IUnitOfMeasureRepository
{
    Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitOfMeasure?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitOfMeasure>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(UnitOfMeasure uom, CancellationToken cancellationToken = default);
    void Update(UnitOfMeasure uom);
    void Remove(UnitOfMeasure uom);
}

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Warehouse>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    void Update(Warehouse warehouse);
    void Remove(Warehouse warehouse);
}

public interface IBankAccountRepository
{
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankAccount>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(BankAccount account, CancellationToken cancellationToken = default);
    void Update(BankAccount account);
    void Remove(BankAccount account);
    Task ClearPrimaryFlagAsync(Guid? exceptId, CancellationToken cancellationToken = default);
}

public interface ICustomerGroupRepository
{
    Task<CustomerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerGroup>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerGroup group, CancellationToken cancellationToken = default);
    void Update(CustomerGroup group);
    void Remove(CustomerGroup group);
}
