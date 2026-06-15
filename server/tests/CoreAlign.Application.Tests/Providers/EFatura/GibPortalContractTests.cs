namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class GibPortalContractTests : IEFaturaProviderContractTests<HarnessBackedEFaturaProvider>
{
    protected override HarnessBackedEFaturaProvider CreateProvider(IEFaturaContractTestHarness harness) =>
        new("gib-portal-direct", harness);
}
