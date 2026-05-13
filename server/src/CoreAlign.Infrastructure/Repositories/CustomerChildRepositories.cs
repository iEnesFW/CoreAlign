using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class CustomerAddressRepository : ICustomerAddressRepository
{
    private readonly CoreAlignDbContext _context;

    public CustomerAddressRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<CustomerAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerAddress>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var items = await _context.CustomerAddresses
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.Label)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task AddAsync(CustomerAddress address, CancellationToken cancellationToken = default)
    {
        await _context.CustomerAddresses.AddAsync(address, cancellationToken);
    }

    public void Update(CustomerAddress address) => _context.CustomerAddresses.Update(address);

    public void Remove(CustomerAddress address) => _context.CustomerAddresses.Remove(address);

    public async Task ClearPrimaryAsync(Guid customerId, Guid? excludeAddressId, CancellationToken cancellationToken = default)
    {
        var addresses = await _context.CustomerAddresses
            .Where(a => a.CustomerId == customerId && a.IsPrimary && (excludeAddressId == null || a.Id != excludeAddressId))
            .ToListAsync(cancellationToken);
        foreach (var a in addresses)
        {
            a.IsPrimary = false;
        }
    }
}

public class CustomerContactRepository : ICustomerContactRepository
{
    private readonly CoreAlignDbContext _context;

    public CustomerContactRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<CustomerContact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CustomerContacts.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CustomerContact>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var items = await _context.CustomerContacts
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return items;
    }

    public async Task AddAsync(CustomerContact contact, CancellationToken cancellationToken = default)
    {
        await _context.CustomerContacts.AddAsync(contact, cancellationToken);
    }

    public void Update(CustomerContact contact) => _context.CustomerContacts.Update(contact);

    public void Remove(CustomerContact contact) => _context.CustomerContacts.Remove(contact);

    public async Task ClearPrimaryAsync(Guid customerId, Guid? excludeContactId, CancellationToken cancellationToken = default)
    {
        var contacts = await _context.CustomerContacts
            .Where(c => c.CustomerId == customerId && c.IsPrimary && (excludeContactId == null || c.Id != excludeContactId))
            .ToListAsync(cancellationToken);
        foreach (var c in contacts)
        {
            c.IsPrimary = false;
        }
    }
}
