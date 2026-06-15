using CoreAlign.Domain.Common;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Entities;

/// <summary>
/// Per-tenant subscription to a <see cref="Module"/>. <c>EndUtc</c> null means
/// perpetual (typically core modules); otherwise the access window ends at
/// EndUtc. <see cref="Extend"/> stretches the window forward from the later of
/// "now" or the existing end-date, so renewing early doesn't burn paid time.
/// </summary>
public class TenantModule : TenantEntity
{
    public Guid ModuleId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime? EndUtc { get; private set; }
    public TenantModuleSource Source { get; private set; }
    public string? Notes { get; private set; }

    public bool IsCurrentlyActive => EndUtc == null || EndUtc > DateTime.UtcNow;

    protected TenantModule() { }

    public TenantModule(Guid moduleId, DateTime startUtc, DateTime? endUtc, TenantModuleSource source, string? notes = null)
    {
        if (moduleId == Guid.Empty) throw new ArgumentException("ModuleId is required.", nameof(moduleId));
        if (endUtc.HasValue && endUtc.Value <= startUtc) throw new ArgumentException("EndUtc must be after StartUtc.", nameof(endUtc));

        ModuleId = moduleId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Source = source;
        Notes = notes?.Trim();
    }

    public void Extend(int additionalDays)
    {
        if (additionalDays <= 0) throw new ArgumentOutOfRangeException(nameof(additionalDays), "Days must be positive.");

        var now = DateTime.UtcNow;
        var basis = EndUtc.HasValue && EndUtc.Value > now ? EndUtc.Value : now;
        EndUtc = basis.AddDays(additionalDays);
        UpdatedAtUtc = now;
    }

    public void SetSource(TenantModuleSource source)
    {
        Source = source;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
