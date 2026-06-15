using System.Text;
using CoreAlign.Application.Providers;
using CoreAlign.Application.Providers.CncExport;

namespace CoreAlign.Infrastructure.Providers.CncExport.Mock;

public sealed class MockCncExportProvider : ICncExporter
{
    public string Name => "mock";
    public string DisplayName => "Mock CNC Exporter";
    public ProviderCapabilities Capabilities { get; } = new(
        ProviderCapability.None,
        new Dictionary<string, string> { ["env"] = "dev" });

    public CncExportFormat Format => CncExportFormat.Dxf;
    public CncDomain Domain => CncDomain.ProfileCut1D;

    public Task<CncExportResult> ExportAsync(CuttingPlanSnapshot plan, CncExportOptions opts, CancellationToken ct)
    {
        const string dxfStub = "0\nSECTION\n2\nENTITIES\n0\nMOCK\n0\nENDSEC\n0\nEOF\n";
        var bytes = Encoding.ASCII.GetBytes(dxfStub);
        var fileName = $"mock-cnc-{plan.PlanId:N}.dxf";
        var result = new CncExportResult(CncExportFormat.Dxf, bytes, "application/dxf", fileName);
        return Task.FromResult(result);
    }
}
