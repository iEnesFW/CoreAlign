using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly CoreAlignDbContext _context;
    public BrandRepository(CoreAlignDbContext context) => _context = context;

    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Brand>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Brands.AsNoTracking();
        if (isActive.HasValue) query = query.Where(b => b.IsActive == isActive.Value);
        return await query.OrderBy(b => b.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default) =>
        await _context.Brands.AddAsync(brand, cancellationToken);
    public void Update(Brand brand) => _context.Brands.Update(brand);
    public void Remove(Brand brand) => _context.Brands.Remove(brand);
}

public class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly CoreAlignDbContext _context;
    public ProductCategoryRepository(CoreAlignDbContext context) => _context = context;

    public Task<ProductCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductCategory>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ProductCategories.AsNoTracking();
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductCategory>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default) =>
        await _context.ProductCategories.AsNoTracking().Where(c => c.ParentCategoryId == parentId).OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
        await _context.ProductCategories.AddAsync(category, cancellationToken);
    public void Update(ProductCategory category) => _context.ProductCategories.Update(category);
    public void Remove(ProductCategory category) => _context.ProductCategories.Remove(category);
}

public class CustomerGroupRepository : ICustomerGroupRepository
{
    private readonly CoreAlignDbContext _context;
    public CustomerGroupRepository(CoreAlignDbContext context) => _context = context;

    public Task<CustomerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.CustomerGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerGroup>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CustomerGroups.AsNoTracking();
        if (isActive.HasValue) query = query.Where(g => g.IsActive == isActive.Value);
        return await query.OrderBy(g => g.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CustomerGroup group, CancellationToken cancellationToken = default) =>
        await _context.CustomerGroups.AddAsync(group, cancellationToken);
    public void Update(CustomerGroup group) => _context.CustomerGroups.Update(group);
    public void Remove(CustomerGroup group) => _context.CustomerGroups.Remove(group);
}

public class UnitOfMeasureRepository : IUnitOfMeasureRepository
{
    private readonly CoreAlignDbContext _context;
    public UnitOfMeasureRepository(CoreAlignDbContext context) => _context = context;

    public Task<UnitOfMeasure?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<UnitOfMeasure?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Code == code, cancellationToken);

    public async Task<IReadOnlyList<UnitOfMeasure>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.UnitsOfMeasure.AsNoTracking();
        if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);
        return await query.OrderBy(u => u.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UnitOfMeasure uom, CancellationToken cancellationToken = default) =>
        await _context.UnitsOfMeasure.AddAsync(uom, cancellationToken);
    public void Update(UnitOfMeasure uom) => _context.UnitsOfMeasure.Update(uom);
    public void Remove(UnitOfMeasure uom) => _context.UnitsOfMeasure.Remove(uom);
}

public class TaxRateRepository : ITaxRateRepository
{
    private readonly CoreAlignDbContext _context;
    public TaxRateRepository(CoreAlignDbContext context) => _context = context;

    public Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.TaxRates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TaxRate?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.TaxRates.FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

    public async Task<IReadOnlyList<TaxRate>> ListAsync(bool? isActive = null, bool? isWithholding = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TaxRates.AsNoTracking();
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        if (isWithholding.HasValue) query = query.Where(t => t.IsWithholding == isWithholding.Value);
        return await query.OrderBy(t => t.RatePercent).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaxRate taxRate, CancellationToken cancellationToken = default) =>
        await _context.TaxRates.AddAsync(taxRate, cancellationToken);
    public void Update(TaxRate taxRate) => _context.TaxRates.Update(taxRate);
    public void Remove(TaxRate taxRate) => _context.TaxRates.Remove(taxRate);
}

public class PaymentTermRepository : IPaymentTermRepository
{
    private readonly CoreAlignDbContext _context;
    public PaymentTermRepository(CoreAlignDbContext context) => _context = context;

    public Task<PaymentTerm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PaymentTerms.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PaymentTerm>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PaymentTerms.AsNoTracking();
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);
        return await query.OrderBy(p => p.NetDays).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PaymentTerm term, CancellationToken cancellationToken = default) =>
        await _context.PaymentTerms.AddAsync(term, cancellationToken);
    public void Update(PaymentTerm term) => _context.PaymentTerms.Update(term);
    public void Remove(PaymentTerm term) => _context.PaymentTerms.Remove(term);
}

public class PriceListRepository : IPriceListRepository
{
    private readonly CoreAlignDbContext _context;
    public PriceListRepository(CoreAlignDbContext context) => _context = context;

    public Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PriceLists.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<PriceList?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.PriceLists.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PriceList>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.PriceLists.AsNoTracking();
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);
        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public Task<PriceListItem?> GetItemAsync(Guid priceListId, Guid productId, CancellationToken cancellationToken = default) =>
        _context.PriceListItems.FirstOrDefaultAsync(i => i.PriceListId == priceListId && i.ProductId == productId, cancellationToken);

    public async Task AddAsync(PriceList list, CancellationToken cancellationToken = default) =>
        await _context.PriceLists.AddAsync(list, cancellationToken);
    public async Task AddItemAsync(PriceListItem item, CancellationToken cancellationToken = default) =>
        await _context.PriceListItems.AddAsync(item, cancellationToken);
    public void Update(PriceList list) => _context.PriceLists.Update(list);
    public void UpdateItem(PriceListItem item) => _context.PriceListItems.Update(item);
    public void Remove(PriceList list) => _context.PriceLists.Remove(list);
    public void RemoveItem(PriceListItem item) => _context.PriceListItems.Remove(item);
}

public class WarehouseRepository : IWarehouseRepository
{
    private readonly CoreAlignDbContext _context;
    public WarehouseRepository(CoreAlignDbContext context) => _context = context;

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<Warehouse?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _context.Warehouses.FirstOrDefaultAsync(w => w.IsDefault, cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Warehouses.AsNoTracking();
        if (isActive.HasValue) query = query.Where(w => w.IsActive == isActive.Value);
        return await query.OrderBy(w => w.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default) =>
        await _context.Warehouses.AddAsync(warehouse, cancellationToken);
    public void Update(Warehouse warehouse) => _context.Warehouses.Update(warehouse);
    public void Remove(Warehouse warehouse) => _context.Warehouses.Remove(warehouse);
}

public class BankAccountRepository : IBankAccountRepository
{
    private readonly CoreAlignDbContext _context;
    public BankAccountRepository(CoreAlignDbContext context) => _context = context;

    public Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.BankAccounts.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<BankAccount>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.BankAccounts.AsNoTracking();
        if (isActive.HasValue) query = query.Where(b => b.IsActive == isActive.Value);
        return await query.OrderByDescending(b => b.IsPrimary).ThenBy(b => b.BankName).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BankAccount account, CancellationToken cancellationToken = default) =>
        await _context.BankAccounts.AddAsync(account, cancellationToken);
    public void Update(BankAccount account) => _context.BankAccounts.Update(account);
    public void Remove(BankAccount account) => _context.BankAccounts.Remove(account);

    public Task ClearPrimaryFlagAsync(Guid? exceptId, CancellationToken cancellationToken = default) =>
        _context.BankAccounts
            .Where(b => b.IsPrimary && (exceptId == null || b.Id != exceptId))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsPrimary, false), cancellationToken);
}

public class DocumentSequenceRepository : IDocumentSequenceRepository
{
    private readonly CoreAlignDbContext _context;
    public DocumentSequenceRepository(CoreAlignDbContext context) => _context = context;

    public Task<DocumentSequence?> GetAsync(DocumentSequenceType type, CancellationToken cancellationToken = default) =>
        _context.DocumentSequences.FirstOrDefaultAsync(d => d.Type == type, cancellationToken);

    public async Task AcquireLockAsync(DocumentSequenceType type, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsNpgsql()) return;
        var lockKey = $"docseq:{_context.CurrentTenantIdOrEmpty}:{(int)type}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);
    }

    public async Task<string> ConsumeAsync(DocumentSequenceType type, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await AcquireLockAsync(type, cancellationToken);

        var sequence = await _context.DocumentSequences.FirstOrDefaultAsync(d => d.Type == type, cancellationToken)
            ?? throw new InvalidOperationException($"Document sequence '{type}' is not seeded for current tenant.");

        var rendered = sequence.ConsumeNext(nowUtc);
        return rendered;
    }

    public async Task<string> PeekAsync(DocumentSequenceType type, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var sequence = await _context.DocumentSequences.AsNoTracking().FirstOrDefaultAsync(d => d.Type == type, cancellationToken)
            ?? throw new InvalidOperationException($"Document sequence '{type}' is not seeded for current tenant.");
        return sequence.Peek(nowUtc);
    }

    public async Task EnsureExistsAsync(DocumentSequenceType type, string prefix, int padLength, int year, CancellationToken cancellationToken = default)
    {
        var exists = await _context.DocumentSequences.AnyAsync(d => d.Type == type, cancellationToken);
        if (exists) return;
        await _context.DocumentSequences.AddAsync(new DocumentSequence(type, prefix, year, 1, padLength), cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentSequence>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.DocumentSequences.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(DocumentSequence sequence, CancellationToken cancellationToken = default) =>
        await _context.DocumentSequences.AddAsync(sequence, cancellationToken);

    public void Update(DocumentSequence sequence) => _context.DocumentSequences.Update(sequence);
}
