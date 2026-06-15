namespace CoreAlign.Integration.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<CoreAlignWebApiFactory>
{
    public const string Name = "CoreAlign Integration";
}
