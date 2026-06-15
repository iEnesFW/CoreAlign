using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
    private readonly CoreAlignDbContext _context;
    public TagRepository(CoreAlignDbContext context) => _context = context;

    public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Tag>> ListAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Tags.AsNoTracking();
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive.Value);
        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<Tag>();
        return await _context.Tags.AsNoTracking().Where(t => ids.Contains(t.Id)).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default) =>
        await _context.Tags.AddAsync(tag, cancellationToken);
    public void Update(Tag tag) => _context.Tags.Update(tag);
    public void Remove(Tag tag) => _context.Tags.Remove(tag);
}

public class CustomerTagLinkRepository : ICustomerTagLinkRepository
{
    private readonly CoreAlignDbContext _context;
    public CustomerTagLinkRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<CustomerTagLink>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.CustomerTagLinks.AsNoTracking().Where(l => l.CustomerId == customerId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, List<Tag>>> GetTagsByCustomersAsync(
        IReadOnlyCollection<Guid> customerIds,
        CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
        {
            return new Dictionary<Guid, List<Tag>>();
        }

        var rows = await _context.CustomerTagLinks
            .AsNoTracking()
            .Where(l => customerIds.Contains(l.CustomerId))
            .Join(
                _context.Tags.AsNoTracking(),
                l => l.TagId,
                t => t.Id,
                (l, t) => new { l.CustomerId, Tag = t })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.CustomerId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Tag.Name).Select(r => r.Tag).ToList());
    }

    public async Task SyncAsync(Guid customerId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CustomerTagLinks
            .Where(l => l.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        var desired = tagIds.Distinct().ToHashSet();

        var toRemove = existing.Where(l => !desired.Contains(l.TagId)).ToList();
        if (toRemove.Count > 0)
        {
            _context.CustomerTagLinks.RemoveRange(toRemove);
        }

        var existingTagIds = existing.Select(l => l.TagId).ToHashSet();
        var toAdd = desired.Where(id => !existingTagIds.Contains(id)).Select(id => new CustomerTagLink(customerId, id));
        await _context.CustomerTagLinks.AddRangeAsync(toAdd, cancellationToken);
    }

    public async Task<bool> AttachAsync(Guid customerId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CustomerTagLinks
            .FirstOrDefaultAsync(l => l.CustomerId == customerId && l.TagId == tagId, cancellationToken);
        if (existing is not null)
        {
            return false;
        }
        await _context.CustomerTagLinks.AddAsync(new CustomerTagLink(customerId, tagId), cancellationToken);
        return true;
    }

    public async Task<bool> DetachAsync(Guid customerId, Guid tagId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CustomerTagLinks
            .FirstOrDefaultAsync(l => l.CustomerId == customerId && l.TagId == tagId, cancellationToken);
        if (existing is null)
        {
            return false;
        }
        _context.CustomerTagLinks.Remove(existing);
        return true;
    }

    public async Task ReassignCustomerAsync(Guid sourceCustomerId, Guid targetCustomerId, CancellationToken cancellationToken = default)
    {
        var sourceLinks = await _context.CustomerTagLinks
            .Where(l => l.CustomerId == sourceCustomerId)
            .ToListAsync(cancellationToken);
        if (sourceLinks.Count == 0)
        {
            return;
        }

        var targetTagIds = await _context.CustomerTagLinks
            .Where(l => l.CustomerId == targetCustomerId)
            .Select(l => l.TagId)
            .ToListAsync(cancellationToken);
        var targetSet = targetTagIds.ToHashSet();

        foreach (var link in sourceLinks)
        {
            if (targetSet.Contains(link.TagId))
            {
                _context.CustomerTagLinks.Remove(link);
            }
            else
            {
                _context.CustomerTagLinks.Remove(link);
                await _context.CustomerTagLinks.AddAsync(new CustomerTagLink(targetCustomerId, link.TagId) { TenantId = link.TenantId }, cancellationToken);
                targetSet.Add(link.TagId);
            }
        }
    }
}
