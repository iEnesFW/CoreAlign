namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class NilveraContractTests : IEFaturaProviderContractTests<HarnessBackedEFaturaProvider>
{
    protected override HarnessBackedEFaturaProvider CreateProvider(IEFaturaContractTestHarness harness) =>
        new("nilvera", harness);
}
