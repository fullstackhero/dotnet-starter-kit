using FSH.Modules.Files.Contracts;
using FSH.Modules.Files.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// Coverage for <see cref="FileAccessPolicyExtensions.AddFileAccessPolicy{TPolicy}"/> — the DI sugar
/// owning modules call in their ConfigureServices. Verifies the registration lands as a scoped
/// <see cref="IFileAccessPolicy"/> that the <see cref="FileAccessPolicyRegistry"/> resolves by
/// OwnerType, and that the null-services guard fires.
/// </summary>
public sealed class FileAccessPolicyExtensionsTests
{
    private sealed class StubPolicy : IFileAccessPolicy
    {
        public string OwnerType => "ExtensionsTestOwner";
        public Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
            => Task.FromResult(true);
        public Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
            => Task.FromResult(true);
        public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }

    #region Happy Path

    [Fact]
    public void AddFileAccessPolicy_Should_Register_Scoped_IFileAccessPolicy()
    {
        var services = new ServiceCollection();

        var returned = services.AddFileAccessPolicy<StubPolicy>();

        returned.ShouldBeSameAs(services); // fluent return
        var descriptor = services.ShouldHaveSingleItem();
        descriptor.ServiceType.ShouldBe(typeof(IFileAccessPolicy));
        descriptor.ImplementationType.ShouldBe(typeof(StubPolicy));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddFileAccessPolicy_Should_Be_Resolvable_By_Registry_Via_OwnerType()
    {
        var services = new ServiceCollection();
        services.AddFileAccessPolicy<StubPolicy>();
        services.AddScoped<FileAccessPolicyRegistry>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<FileAccessPolicyRegistry>();

        var resolved = registry.Resolve("ExtensionsTestOwner");

        resolved.ShouldNotBeNull();
        resolved.ShouldBeOfType<StubPolicy>();
        // Lookup is case-insensitive.
        registry.Resolve("extensionstestowner").ShouldNotBeNull();
        // Unknown owner type → null (closed-by-default).
        registry.Resolve("nope").ShouldBeNull();
    }

    #endregion

    #region Exception

    [Fact]
    public void AddFileAccessPolicy_Should_Throw_When_Services_Null()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() => services.AddFileAccessPolicy<StubPolicy>());
    }

    #endregion
}
