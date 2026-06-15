namespace CoreAlign.Application.Providers.CadImport;

public interface ICadImporter : IExternalProvider
{
    IReadOnlyList<string> SupportedExtensions { get; }

    Task<CadImportResult> ImportAsync(
        Stream file,
        string fileName,
        CadImportOptions options,
        CancellationToken cancellationToken);
}

public sealed record CadImportOptions(
    bool DefaultUnitsMm,
    string[]? LayerWhitelist,
    string? RunHintLayer);

public sealed record CadRunCandidate(
    string Label,
    int LengthMm,
    int HeightMm,
    decimal OriginX,
    decimal OriginY);

public sealed record CadImportResult(
    CadRunCandidate[] RunCandidates,
    string[] WarningKeys);
