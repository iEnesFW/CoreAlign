namespace CoreAlign.Application.Providers.CncExport;

public enum CncExportFormat
{
    Dxf,
    Nc1,
    GCode,
    Svg
}

public enum CncDomain
{
    ProfileCut1D,
    GlassCut2D,
    AssemblyDrill
}

public sealed record CncPiece(int Length, int? Width, string Label);

public sealed record CuttingPlanSnapshot(
    Guid PlanId,
    IReadOnlyList<CncPiece> Pieces,
    string Material,
    int Quantity);

public sealed record CncExportOptions(
    bool UnitsMm,
    bool IncludeLabels,
    decimal KerfMm);

public sealed record CncExportResult(
    CncExportFormat Format,
    byte[] RawBytes,
    string ContentType,
    string FileName);

public interface ICncExporter : IExternalProvider
{
    CncExportFormat Format { get; }
    CncDomain Domain { get; }
    Task<CncExportResult> ExportAsync(CuttingPlanSnapshot plan, CncExportOptions opts, CancellationToken ct);
}
