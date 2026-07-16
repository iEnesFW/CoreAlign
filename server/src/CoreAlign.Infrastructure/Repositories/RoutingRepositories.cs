using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Manufacturing;
using CoreAlign.Domain.Entities.Payroll;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Repositories;

public sealed class ProductionRoutingRepository : IProductionRoutingRepository
{
    private readonly CoreAlignDbContext _context;

    public ProductionRoutingRepository(CoreAlignDbContext context) => _context = context;

    private DbSet<ProductionRouting> Routings => _context.Set<ProductionRouting>();
    private DbSet<RoutingStep> Steps => _context.Set<RoutingStep>();

    public async Task AddAsync(ProductionRouting routing, CancellationToken cancellationToken = default)
        => await Routings.AddAsync(routing, cancellationToken);

    public void Remove(ProductionRouting routing) => Routings.Remove(routing);

    public void RemoveSteps(IEnumerable<RoutingStep> steps) => Steps.RemoveRange(steps);

    public async Task AddStepsAsync(IEnumerable<RoutingStep> steps, CancellationToken cancellationToken = default)
        => await Steps.AddRangeAsync(steps, cancellationToken);

    public Task<ProductionRouting?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => Routings.Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id, cancellationToken);

    public Task<ProductionRouting?> GetByIdReadAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => Routings.AsNoTracking().Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(Guid tenantId, string code, Guid? excludeId, CancellationToken cancellationToken = default)
        => Routings.AnyAsync(
            r => r.TenantId == tenantId && r.Code == code && (excludeId == null || r.Id != excludeId),
            cancellationToken);

    public Task<bool> IsActiveAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => Routings.AnyAsync(
            r => r.TenantId == tenantId && r.Id == id && r.Status == RoutingStatus.Active,
            cancellationToken);

    public Task<bool> IsReferencedByProductAsync(Guid tenantId, Guid routingId, CancellationToken cancellationToken = default)
        => _context.Set<Product>().AnyAsync(
            p => p.TenantId == tenantId && p.RoutingId == routingId, cancellationToken);

    public async Task<IReadOnlyList<RoutingSummaryRow>> ListSummariesAsync(
        Guid tenantId,
        RoutingStatus? status,
        int take,
        CancellationToken cancellationToken = default)
        => await Routings.AsNoTracking()
            .Where(r => r.TenantId == tenantId && (status == null || r.Status == status))
            .OrderByDescending(r => r.UpdatedAtUtc)
            .Take(take)
            .Select(r => new RoutingSummaryRow(
                r.Id, r.Code, r.Name, r.Status, r.Steps.Count, r.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
}

public sealed class WorkCenterOperatorRepository : IWorkCenterOperatorRepository
{
    private readonly CoreAlignDbContext _context;

    public WorkCenterOperatorRepository(CoreAlignDbContext context) => _context = context;

    private DbSet<WorkCenterOperator> Operators => _context.Set<WorkCenterOperator>();

    public async Task AddAsync(WorkCenterOperator op, CancellationToken cancellationToken = default)
        => await Operators.AddAsync(op, cancellationToken);

    public Task<WorkCenterOperator?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => Operators.FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == id, cancellationToken);

    public Task<bool> ActiveAssignmentExistsAsync(
        Guid tenantId,
        Guid workCenterId,
        Guid employeeId,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
        => Operators.AnyAsync(
            o => o.TenantId == tenantId
                && o.WorkCenterId == workCenterId
                && o.EmployeeId == employeeId
                && o.IsActive
                && (excludeId == null || o.Id != excludeId),
            cancellationToken);

    public Task<WorkCenterOperatorRow?> GetRowByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => Project(Operators.AsNoTracking().Where(o => o.TenantId == tenantId && o.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkCenterOperatorRow>> ListAsync(
        Guid tenantId,
        Guid? workCenterId,
        Guid? employeeId,
        int take,
        CancellationToken cancellationToken = default)
        => await Project(Operators.AsNoTracking()
                .Where(o => o.TenantId == tenantId
                    && (workCenterId == null || o.WorkCenterId == workCenterId)
                    && (employeeId == null || o.EmployeeId == employeeId))
                .OrderByDescending(o => o.IsActive)
                .ThenByDescending(o => o.IsPrimary)
                .Take(take))
            .ToListAsync(cancellationToken);

    private IQueryable<WorkCenterOperatorRow> Project(IQueryable<WorkCenterOperator> source)
        => from o in source
           join w in _context.Set<WorkCenter>() on o.WorkCenterId equals w.Id into wj
           from w in wj.DefaultIfEmpty()
           join e in _context.Set<Employee>() on o.EmployeeId equals e.Id into ej
           from e in ej.DefaultIfEmpty()
           select new WorkCenterOperatorRow(
               o.Id,
               o.WorkCenterId,
               w != null ? w.Code : string.Empty,
               w != null ? w.Name : string.Empty,
               o.EmployeeId,
               e != null ? e.FirstName + " " + e.LastName : string.Empty,
               e != null && !e.IsDeleted && e.Status != EmploymentStatus.Terminated,
               o.QualificationLevel,
               o.IsPrimary,
               o.IsActive,
               o.CertifiedOn,
               o.Notes);
}
