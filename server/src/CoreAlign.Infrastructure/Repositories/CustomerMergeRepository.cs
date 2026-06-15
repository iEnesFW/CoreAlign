using CoreAlign.Application.Customers.Merge;
using CoreAlign.Domain.Entities.Customers;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class CustomerMergeOperationRepository : ICustomerMergeOperationRepository
{
    private readonly CoreAlignDbContext _context;

    public CustomerMergeOperationRepository(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<CustomerMergeLog?> GetByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) =>
        _context.Set<CustomerMergeLog>().FirstOrDefaultAsync(l => l.OperationId == operationId, cancellationToken);

    public async Task AddAsync(CustomerMergeLog log, CancellationToken cancellationToken = default) =>
        await _context.Set<CustomerMergeLog>().AddAsync(log, cancellationToken);
}

public sealed class CustomerMergeReassignmentService : ICustomerMergeReassignmentService
{
    private readonly CoreAlignDbContext _context;
    private readonly ICustomerTagLinkRepository _tagLinks;

    public CustomerMergeReassignmentService(CoreAlignDbContext context, ICustomerTagLinkRepository tagLinks)
    {
        _context = context;
        _tagLinks = tagLinks;
    }

    public async Task<CustomerMergeCounts> ReassignAsync(Guid sourceCustomerId, Guid targetCustomerId, CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.CustomerId, _ => targetCustomerId), cancellationToken);

        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.CustomerId, _ => targetCustomerId), cancellationToken);

        var payments = await _context.Payments
            .Where(p => p.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CustomerId, _ => targetCustomerId), cancellationToken);

        var addresses = await _context.CustomerAddresses
            .Where(a => a.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.CustomerId, _ => targetCustomerId)
                .SetProperty(a => a.IsPrimary, _ => false), cancellationToken);

        var contacts = await _context.CustomerContacts
            .Where(c => c.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.CustomerId, _ => targetCustomerId)
                .SetProperty(c => c.IsPrimary, _ => false), cancellationToken);

        var comments = await _context.Comments
            .Where(c => c.EntityType == "Customer" && c.EntityId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.EntityId, _ => targetCustomerId), cancellationToken);

        var ledgerEntries = await _context.CustomerLedgerEntries
            .Where(e => e.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.CustomerId, _ => targetCustomerId), cancellationToken);

        var transactions = await _context.CustomerTransactions
            .Where(t => t.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CustomerId, _ => targetCustomerId), cancellationToken);

        var preReassignTagLinkCount = await _context.CustomerTagLinks
            .CountAsync(l => l.CustomerId == sourceCustomerId, cancellationToken);
        await _tagLinks.ReassignCustomerAsync(sourceCustomerId, targetCustomerId, cancellationToken);
        var tagLinks = preReassignTagLinkCount;

        var dealerLinks = await _context.DealerCustomerLinks
            .Where(d => d.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.CustomerId, _ => targetCustomerId), cancellationToken);

        var customerUsers = await _context.CustomerUsers
            .Where(u => u.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.CustomerId, _ => targetCustomerId), cancellationToken);

        var quotes = await _context.Quotes
            .Where(q => q.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(q => q.CustomerId, _ => targetCustomerId), cancellationToken);

        var returns = await _context.ReturnRequests
            .Where(r => r.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.CustomerId, _ => targetCustomerId), cancellationToken);

        var shipments = await _context.Shipments
            .Where(sh => sh.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(sh => sh.CustomerId, _ => targetCustomerId), cancellationToken);

        var customerPrices = await _context.CustomerProductPrices
            .Where(p => p.CustomerId == sourceCustomerId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CustomerId, _ => targetCustomerId), cancellationToken);

        var other = quotes + returns + shipments + customerPrices;

        return new CustomerMergeCounts(
            orders, invoices, payments, addresses, contacts, comments,
            ledgerEntries, transactions, tagLinks, dealerLinks, customerUsers, other);
    }
}
