using CoreAlign.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoreAlign.Application.GlassEnclosure.BomFreshness;

public class BomStaleSignal : IBomStaleSignal
{
    private readonly IGlassProjectRepository _projects;
    private readonly ILogger<BomStaleSignal> _logger;

    public BomStaleSignal(IGlassProjectRepository projects, ILogger<BomStaleSignal> logger)
    {
        _projects = projects;
        _logger = logger;
    }

    public async Task SignalStaleAsync(Guid projectId, BomStaleReason reason, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("BomStaleSignal: project {ProjectId} not found", projectId);
            return;
        }
        project.MarkBomStale(reason.ToString(), DateTime.UtcNow);
        _projects.Update(project);
    }

    public async Task SignalFreshAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null) return;
        project.MarkBomFresh(DateTime.UtcNow);
        _projects.Update(project);
    }
}
