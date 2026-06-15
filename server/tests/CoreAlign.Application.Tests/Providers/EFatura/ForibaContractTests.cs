namespace CoreAlign.Application.Tests.Providers.EFatura;

public sealed class ForibaContractTests : IEFaturaProviderContractTests<HarnessBackedEFaturaProvider>
{
    protected override HarnessBackedEFaturaProvider CreateProvider(IEFaturaContractTestHarness harness) =>
        new("foriba", harness);
}
