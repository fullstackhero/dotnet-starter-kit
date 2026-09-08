using Microsoft.EntityFrameworkCore;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// Registers the framework's provider conventions on a <see cref="DbContext"/>.
/// </summary>
public static class ModelConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="HeroProviderConventions"/> so portable column and index intent is resolved
    /// against <paramref name="provider"/> when the model is finalized.
    /// </summary>
    /// <remarks>
    /// Contexts deriving from <c>BaseDbContext</c> get this automatically. Contexts that do not —
    /// <c>IdentityDbContext</c> and <c>TenantDbContext</c>, which have their own EF base classes —
    /// must call it from their own <c>ConfigureConventions</c> override.
    /// </remarks>
    public static ModelConfigurationBuilder AddHeroProviderConventions(
        this ModelConfigurationBuilder configurationBuilder,
        string? provider)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new HeroProviderConventions(provider));
        return configurationBuilder;
    }
}
