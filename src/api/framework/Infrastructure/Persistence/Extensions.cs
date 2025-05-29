using FSH.Framework.Core.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Data;
using Npgsql;
using Microsoft.Data.SqlClient;
using FSH.Framework.Infrastructure.Persistence.Migrations;

namespace FSH.Framework.Infrastructure.Persistence;
public static class Extensions
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Extensions));

    public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
            .ValidateDataAnnotations()
            .PostConfigure(config =>
            {
                Logger.Information("current db provider: {DatabaseProvider}", config.Provider);
                Logger.Information("for documentations and guides, visit https://www.fullstackhero.net");
                Logger.Information("to sponsor this project, visit https://opencollective.com/fullstackhero");
            });

        builder.Services.AddScoped<IDbConnection>(sp =>
        {
            var dbConfig = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return dbConfig.Provider.ToUpperInvariant() switch
            {
                DbProviders.PostgreSQL => new NpgsqlConnection(dbConfig.ConnectionString),
                DbProviders.MSSQL => new SqlConnection(dbConfig.ConnectionString),
                _ => throw new InvalidOperationException($"DB Provider {dbConfig.Provider} is not supported."),
            };
        });

        // Register migration runner
        builder.Services.AddScoped<MigrationRunner>();

        return builder;
    }

    public static async Task<WebApplication> RunMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
        
        try
        {
            await migrationRunner.RunMigrationsAsync();
            Logger.Information("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to run database migrations");
            throw;
        }

        return app;
    }
}
