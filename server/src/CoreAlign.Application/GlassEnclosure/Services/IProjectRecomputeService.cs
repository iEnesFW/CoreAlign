using CoreAlign.Domain.Entities.GlassEnclosure;

namespace CoreAlign.Application.GlassEnclosure.Services;

public interface IProjectRecomputeService
{
    Task RecalculateAsync(GlassProject project, CancellationToken cancellationToken = default);
}

public class ProjectRecomputeService : IProjectRecomputeService
{
    private readonly IBOMComposer _composer;

    public ProjectRecomputeService(IBOMComposer composer) => _composer = composer;

    public async Task RecalculateAsync(GlassProject project, CancellationToken cancellationToken = default)
    {
        var composition = await _composer.ComposeAsync(project, cancellationToken);
        project.RecordCalculations(
            composition.TotalAreaM2,
            composition.TotalPanels,
            project.WindLoadPaCalculated ?? 0m,
            project.WeightedUValue ?? 0m,
            project.WeightedSoundDb ?? 0m);
        project.RecordTotals(composition.Subtotal, 0m, composition.TaxAmount, composition.GrandTotal);
    }
}
