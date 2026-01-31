using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using FSH.Framework.Core.Domain;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Persistence.Context;

/// <summary>
/// Base database context with multi-tenancy and soft delete support.
/// </summary>
/// <param name="multiTenantContextAccessor">Accessor for multi-tenant context information.</param>
/// <param name="options">Database context options.</param>
/// <param name="settings">Database configuration settings.</param>
/// <param name="environment">Host environment information.</param>
public class BaseDbContext(IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    DbContextOptions options,
    IOptions<DatabaseOptions> settings,
    IHostEnvironment environment)
    : MultiTenantDbContext(multiTenantContextAccessor, options)
{
    private readonly DatabaseOptions _settings = settings.Value;

    /// <summary>
    /// Configures the model by applying global query filters for soft delete functionality.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the database schema.</param>
    /// <exception cref="ArgumentNullException">Thrown when modelBuilder is null.</exception>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.AppendGlobalQueryFilter<ISoftDeletable>(s => !s.IsDeleted);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Configures the database connection using tenant-specific connection string if available.
    /// </summary>
    /// <param name="optionsBuilder">The options builder for configuring the database connection.</param>
    /// <exception cref="ArgumentNullException">Thrown when optionsBuilder is null.</exception>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        if (!string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext.TenantInfo?.ConnectionString))
        {
            optionsBuilder.ConfigureHeroDatabase(
                _settings.Provider,
                multiTenantContextAccessor.MultiTenantContext.TenantInfo.ConnectionString!,
                _settings.MigrationsAssembly,
                environment.IsDevelopment());
        }
    }

    /// <summary>
    /// Saves all changes made in this context to the database with tenant overwrite mode.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the save operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TenantNotSetMode = TenantNotSetMode.Overwrite;
        int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}
