using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public class GLPostingMappingRepository : IGLPostingMappingRepository
{
    private readonly CoreAlignDbContext _context;
    public GLPostingMappingRepository(CoreAlignDbContext context) => _context = context;

    public async Task<IReadOnlyList<GLPostingMapping>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.GLPostingMappings.AsNoTracking().OrderBy(m => m.PostingKey).ToListAsync(cancellationToken);

    public Task<GLPostingMapping?> GetByKeyAsync(GLPostingKey postingKey, CancellationToken cancellationToken = default) =>
        _context.GLPostingMappings.FirstOrDefaultAsync(m => m.PostingKey == postingKey, cancellationToken);

    public async Task AddAsync(GLPostingMapping mapping, CancellationToken cancellationToken = default) =>
        await _context.GLPostingMappings.AddAsync(mapping, cancellationToken);

    public void Update(GLPostingMapping mapping) => _context.GLPostingMappings.Update(mapping);
}
