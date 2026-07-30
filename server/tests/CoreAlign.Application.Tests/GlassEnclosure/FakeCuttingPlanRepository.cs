using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.Tests.GlassEnclosure;

internal sealed class FakeCuttingPlanRepository : IGlassProjectCuttingPlanRepository
{
    public List<GlassProjectCuttingPlan> Plans { get; } = new();

    public Task<GlassProjectCuttingPlan?> GetLatestAsync(
        Guid projectId, GlassCuttingPlanType planType, CancellationToken cancellationToken = default) =>
        Task.FromResult(Plans.LastOrDefault(p => p.ProjectId == projectId && p.PlanType == planType));

    public Task<IReadOnlyList<GlassProjectCuttingPlan>> ListRecentAsync(
        Guid projectId, GlassCuttingPlanType planType, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlassProjectCuttingPlan>>(
            Plans.Where(p => p.ProjectId == projectId && p.PlanType == planType)
                .Reverse()
                .Take(limit)
                .ToList());

    public Task AddAsync(GlassProjectCuttingPlan plan, CancellationToken cancellationToken = default)
    {
        Plans.Add(plan);
        return Task.CompletedTask;
    }
}
