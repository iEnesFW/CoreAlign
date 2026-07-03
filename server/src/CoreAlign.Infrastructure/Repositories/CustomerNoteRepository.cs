using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class CustomerNoteRepository : ICustomerNoteRepository
{
    private readonly CoreAlignDbContext _context;

    public CustomerNoteRepository(CoreAlignDbContext context) => _context = context;

    public async Task AddAsync(CustomerNote note, CancellationToken cancellationToken = default)
        => await _context.Set<CustomerNote>().AddAsync(note, cancellationToken);

    public async Task<IReadOnlyList<CustomerNote>> GetLatestByCustomerAsync(
        Guid customerId,
        int take,
        CancellationToken cancellationToken = default)
        => await _context.Set<CustomerNote>()
            .AsNoTracking()
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
}
