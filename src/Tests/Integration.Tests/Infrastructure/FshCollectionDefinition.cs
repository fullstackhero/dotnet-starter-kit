namespace Integration.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class FshCollectionDefinition : ICollectionFixture<FshWebApplicationFactory>
{
    public const string Name = "FshIntegration";
}
