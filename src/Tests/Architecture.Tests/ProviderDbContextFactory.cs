using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Architecture.Tests;

/// <summary>
/// Builds any of the framework's DbContexts against a chosen provider, without a database.
/// </summary>
/// <remarks>
/// The model builds lazily on first access, so nothing here opens a connection — the connection
/// strings only have to parse. That is what lets a migration guard run in the Docker-free unit-test
/// job.
/// </remarks>
internal static class ProviderDbContextFactory
{
    /// <summary>The migrations assembly that belongs to each provider.</summary>
    public static string MigrationsAssemblyFor(string provider) =>
        provider == DbProviders.MSSQL
            ? "FSH.Starter.Migrations.MSSQL"
            : "FSH.Starter.Migrations.PostgreSQL";

    /// <summary>
    /// Constructs <paramref name="dbContextType"/> wired to <paramref name="provider"/>.
    /// </summary>
    public static DbContext Create(Type dbContextType, string provider)
    {
        ArgumentNullException.ThrowIfNull(dbContextType);

        var optionsType = typeof(DbContextOptions<>).MakeGenericType(dbContextType);
        var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;

        // ConfigureHeroDatabase rather than a bare UseNpgsql/UseSqlServer: it is the only thing that
        // sets the provider, the MigrationsAssembly AND (on MSSQL) UseCompatibilityLevel(170)
        // together. Without the migrations assembly EF looks for the snapshot in the context's own
        // assembly, finds none, and a drift check silently becomes meaningless. Without the
        // compatibility level, JSON columns map to nvarchar(max) instead of the native json type and
        // every JSON column reports phantom drift.
        builder.ConfigureHeroDatabase(
            provider,
            UnreachableConnectionStringFor(provider),
            MigrationsAssemblyFor(provider),
            isDevelopment: false);

        var options = builder.Options;

        // Empty on purpose: BaseDbContext.OnConfiguring returns early when the tenant connection
        // string is blank, so it never re-wires the provider we just configured.
        var settings = Options.Create(new DatabaseOptions
        {
            Provider = provider,
            ConnectionString = string.Empty,
            MigrationsAssembly = MigrationsAssemblyFor(provider),
        });

        // Nine contexts take the BaseDbContext-shaped four-arg constructor; BillingDbContext and
        // TenantDbContext derive from other EF base classes and take only their options.
        var wideCtor = dbContextType.GetConstructor([
            typeof(IMultiTenantContextAccessor<AppTenantInfo>),
            optionsType,
            typeof(IOptions<DatabaseOptions>),
            typeof(IHostEnvironment),
        ]);

        if (wideCtor is not null)
        {
            return (DbContext)wideCtor.Invoke([
                new StubAccessor(),
                options,
                settings,
                new StubEnvironment(),
            ]);
        }

        var optionsOnlyCtor = dbContextType.GetConstructor([optionsType]);
        if (optionsOnlyCtor is not null)
        {
            return (DbContext)optionsOnlyCtor.Invoke([options]);
        }

        // Fail loudly rather than skipping the context — a silently unchecked context is exactly the
        // hole this guard exists to close.
        throw new InvalidOperationException(
            $"{dbContextType.Name} has neither the four-argument BaseDbContext constructor nor a "
            + $"({optionsType.Name}) constructor, so the migration guard cannot build it. Add the new "
            + "shape to ProviderDbContextFactory.");
    }

    private static string UnreachableConnectionStringFor(string provider) =>
        provider == DbProviders.MSSQL
            ? "Server=arch;Database=arch;Trusted_Connection=True;TrustServerCertificate=True"
            : "Host=arch;Database=arch;Username=arch;Password=arch";

    private sealed class StubAccessor : IMultiTenantContextAccessor<AppTenantInfo>
    {
        // IdentityDbContext dereferences TenantInfo in its constructor, so it must be non-null.
        public IMultiTenantContext<AppTenantInfo> MultiTenantContext { get; set; } =
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("arch", "arch", string.Empty, "arch@arch", "arch"));

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => MultiTenantContext;
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "arch";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
