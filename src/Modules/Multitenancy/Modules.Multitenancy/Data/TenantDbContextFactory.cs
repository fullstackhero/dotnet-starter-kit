using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FSH.Modules.Multitenancy.Data;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    /// <summary>
    /// SQL Server compatibility level the MSSQL provider targets — must match
    /// <c>OptionsBuilderExtensions</c>, or migrations scaffolded here would use
    /// <c>nvarchar(max)</c> for JSON columns while the running app expects the native
    /// <c>json</c> type.
    /// </summary>
    private const int MssqlCompatibilityLevel = 170;

    public TenantDbContext CreateDbContext(string[] args)
    {
        // Design-time factory: read configuration (appsettings + env vars) to decide provider and connection.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = configuration["DatabaseOptions:Provider"] ?? DbProviders.PostgreSQL;
        var migrationsAssembly = configuration["DatabaseOptions:MigrationsAssembly"]
            ?? "FSH.Starter.Migrations.PostgreSQL";
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        var configured = configuration["DatabaseOptions:ConnectionString"];

        switch (provider.ToUpperInvariant())
        {
            case DbProviders.PostgreSQL:
                var postgres = configured
                    ?? "Host=localhost;Database=fsh-tenant;Username=postgres;Password=postgres";
                optionsBuilder.UseNpgsql(
                    postgres,
                    b => b.MigrationsAssembly(migrationsAssembly));
                break;

            case DbProviders.MSSQL:
                // Trusted connection by default: scaffolding never opens a connection, and this
                // avoids shipping a credential literal for an instance that does not exist.
                var sqlServer = configured
                    ?? "Server=localhost;Database=fsh-tenant;Trusted_Connection=True;TrustServerCertificate=True";
                optionsBuilder.UseSqlServer(
                    sqlServer,
                    b =>
                    {
                        b.MigrationsAssembly(migrationsAssembly);
                        b.UseCompatibilityLevel(MssqlCompatibilityLevel);
                    });
                break;

            default:
                throw new NotSupportedException($"Database provider '{provider}' is not supported for TenantDbContext migrations.");
        }

        return new TenantDbContext(optionsBuilder.Options);
    }
}
