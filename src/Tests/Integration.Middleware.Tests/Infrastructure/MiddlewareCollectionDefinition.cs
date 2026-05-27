namespace Integration.Middleware.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class MiddlewareCollectionDefinition : ICollectionFixture<MiddlewareWebApplicationFactory>
{
    public const string Name = "MiddlewareIntegration";
}
