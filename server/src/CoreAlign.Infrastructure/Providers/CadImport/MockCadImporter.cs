using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.CadImport;

namespace CoreAlign.Infrastructure.Providers.CadImport.Mock;

public sealed class MockCadImporter : ICadImporter
{
    private static readonly IReadOnlyList<string> Extensions = new[] { "dxf", "dwg", "ifc", "skp" };

    public string Name => "mock";

    public string DisplayName => "Mock CAD Importer";

    public ProviderCapabilities Capabilities { get; } = new(
        ProviderCapability.None,
        new Dictionary<string, string> { ["env"] = "dev" });

    public IReadOnlyList<string> SupportedExtensions => Extensions;

    public Task<CadImportResult> ImportAsync(
        Stream file,
        string fileName,
        CadImportOptions options,
        CancellationToken cancellationToken)
    {
        var candidates = new[]
        {
            new CadRunCandidate("Run-A", 3000, 2400, 0m, 0m),
            new CadRunCandidate("Run-B", 1800, 2400, 3000m, 0m)
        };

        var result = new CadImportResult(candidates, Array.Empty<string>());
        return Task.FromResult(result);
    }
}
