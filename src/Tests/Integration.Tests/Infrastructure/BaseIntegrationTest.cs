using FSH.Tests.Shared.Infrastructure;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FSH.Tests.Integration.Infrastructure;

[Collection("Integration")]
public abstract class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory>
{
    protected ISender Mediator { get; }
    protected IServiceScope Scope { get; }

    protected BaseIntegrationTest(CustomWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Scope = factory.Services.CreateScope();
        Mediator = Scope.ServiceProvider.GetRequiredService<ISender>();
    }
}

[CollectionDefinition("Integration")]
public class IntegrationFixture : ICollectionFixture<CustomWebApplicationFactory> { }

