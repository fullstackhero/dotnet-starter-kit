using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FSH.Framework.Infrastructure.Persistence.Migrations;

public class MigrationRunner
{
    private readonly IDbConnection _connection;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(IDbConnection connection, ILogger<MigrationRunner> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task RunMigrationsAsync()
    {
        var databaseCreated = await EnsureDatabaseExistsAsync();
        
        // Run migrations if database was just created, migration table doesn't exist, or migration table is empty
        var migrationTableExists = await MigrationTableExistsAsync();
        var hasMigrations = migrationTableExists && await HasAppliedMigrationsAsync();
        
        if (databaseCreated || !migrationTableExists || !hasMigrations)
        {
            await EnsureMigrationTableExistsAsync();
            await RunPendingMigrationsAsync();
        }
        else
        {
            _logger.LogInformation("Database and migrations already exist, skipping migration run");
        }
    }

    private async Task<bool> EnsureDatabaseExistsAsync()
    {
        if (_connection is not NpgsqlConnection npgsqlConnection)
            return false;

        var builder = new NpgsqlConnectionStringBuilder(npgsqlConnection.ConnectionString);
        var databaseName = builder.Database;
        
        // Connect to postgres database to check/create target database
        builder.Database = "postgres";
        
        try
        {
            using var adminConnection = new NpgsqlConnection(builder.ConnectionString);
            await adminConnection.OpenAsync();
            
            var exists = await adminConnection.QuerySingleAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @DatabaseName)",
                new { DatabaseName = databaseName });
            
            if (!exists)
            {
                _logger.LogInformation("Database {DatabaseName} does not exist, creating it", databaseName);
                await adminConnection.ExecuteAsync($"CREATE DATABASE \"{databaseName}\"");
                _logger.LogInformation("Database {DatabaseName} created successfully", databaseName);
                return true; // Database was created
            }
            else
            {
                _logger.LogInformation("Database {DatabaseName} already exists", databaseName);
                return false; // Database already existed
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure database exists, continuing anyway");
            return false;
        }
    }

    private async Task<bool> MigrationTableExistsAsync()
    {
        try
        {
            var count = await _connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '__migrations'");
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in MigrationTableExistsAsync");
            return false;
        }
    }

    private async Task RunPendingMigrationsAsync()
    {
        var scriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        if (!Directory.Exists(scriptsPath))
        {
            _logger.LogWarning("Scripts directory not found at {Path}", scriptsPath);
            return;
        }

        var sqlFiles = Directory.GetFiles(scriptsPath, "*.sql")
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToArray();

        if (sqlFiles.Length == 0)
        {
            _logger.LogInformation("No migration scripts found");
            return;
        }

        foreach (var file in sqlFiles)
        {
            var fileName = Path.GetFileName(file);
            
            if (await IsMigrationAppliedAsync(fileName))
            {
                _logger.LogInformation("Migration {FileName} already applied, skipping", fileName);
                continue;
            }

            _logger.LogInformation("Applying migration {FileName}", fileName);
            
            var sql = await File.ReadAllTextAsync(file);
            await _connection.ExecuteAsync(sql);
            await RecordMigrationAsync(fileName);
            
            _logger.LogInformation("Migration {FileName} applied successfully", fileName);
        }
    }

    private async Task EnsureMigrationTableExistsAsync()
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                id SERIAL PRIMARY KEY,
                migration_name VARCHAR(255) NOT NULL UNIQUE,
                applied_at TIMESTAMP DEFAULT now()
            )";
        
        await _connection.ExecuteAsync(sql);
        _logger.LogInformation("Migration table ensured");
    }

    private async Task<bool> IsMigrationAppliedAsync(string migrationName)
    {
        try
        {
            var sql = "SELECT COUNT(1) FROM __migrations WHERE migration_name = @MigrationName";
            var count = await _connection.QuerySingleAsync<int>(sql, new { MigrationName = migrationName });
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in HasMigrationBeenAppliedAsync");
            return false;
        }
    }

    private async Task RecordMigrationAsync(string migrationName)
    {
        var sql = "INSERT INTO __migrations (migration_name) VALUES (@MigrationName)";
        await _connection.ExecuteAsync(sql, new { MigrationName = migrationName });
    }

    private async Task<bool> HasAppliedMigrationsAsync()
    {
        try
        {
            var sql = "SELECT COUNT(1) FROM __migrations";
            var count = await _connection.QuerySingleAsync<int>(sql);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in HasAppliedMigrationsAsync");
            return false;
        }
    }
}