using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Respawn;
using System.Data.Common;
using Npgsql;
using Xunit;

namespace FSH.Starter.Tests.Integration.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<FSH.Starter.WebApi.Host.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("fsh_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithCleanUp(true)
        .Build();

    private DbConnection _dbConnection = default!;
    private Respawner _respawner = default!;

    public string ConnectionString => _dbContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configBuilder =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseOptions:ConnectionString"] = ConnectionString,
                ["DatabaseOptions:Provider"] = "postgresql",
                ["MernisService:UseDevelopmentMode"] = "true",
                ["MernisService:SimulationDelayMs"] = "50",
                ["JwtOptions:Key"] = "QsJbczCNysv/5SGh+U7sxedX8C07TPQPBdsnSDKZ/aE=",
                ["JwtOptions:Issuer"] = "FSH.Starter.Test",
                ["JwtOptions:Audience"] = "FSH.Starter.Test",
                ["JwtOptions:AccessTokenExpirationMinutes"] = "60",
                ["JwtOptions:RefreshTokenExpirationDays"] = "7"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Override services for testing
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        // Initialize database connection
        _dbConnection = new NpgsqlConnection(ConnectionString);
        await _dbConnection.OpenAsync();

        // Initialize Respawner for database cleanup
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            SchemasToInclude = new[] { "public" },
            DbAdapter = DbAdapter.Postgres
        });

        // Run database migrations
        await RunDatabaseMigrations();
    }

    public new async Task DisposeAsync()
    {
        await _dbConnection.DisposeAsync();
        await _dbContainer.StopAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    private async Task RunDatabaseMigrations()
    {
        // Create database schema
        var createTablesScript = @"
            -- Users table
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                email VARCHAR(255) NOT NULL UNIQUE,
                username VARCHAR(100) NOT NULL UNIQUE,
                phone_number VARCHAR(20) NOT NULL,
                tckn VARCHAR(11) NOT NULL UNIQUE,
                password_hash VARCHAR(255) NOT NULL,
                first_name VARCHAR(100) NOT NULL,
                last_name VARCHAR(100) NOT NULL,
                profession_id INTEGER,
                birth_date DATE,
                member_number VARCHAR(20) UNIQUE,
                is_email_verified BOOLEAN DEFAULT FALSE,
                marketing_consent BOOLEAN DEFAULT FALSE,
                electronic_communication_consent BOOLEAN DEFAULT FALSE,
                membership_agreement_consent BOOLEAN DEFAULT FALSE,
                status VARCHAR(20) DEFAULT 'ACTIVE',
                registration_ip INET,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            );

            -- Roles table
            CREATE TABLE IF NOT EXISTS roles (
                id SERIAL PRIMARY KEY,
                name VARCHAR(50) NOT NULL UNIQUE,
                description VARCHAR(255),
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            );

            -- User roles table
            CREATE TABLE IF NOT EXISTS user_roles (
                user_id UUID REFERENCES users(id) ON DELETE CASCADE,
                role_id INTEGER REFERENCES roles(id) ON DELETE CASCADE,
                assigned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                PRIMARY KEY (user_id, role_id)
            );

            -- Professions table
            CREATE TABLE IF NOT EXISTS professions (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                description VARCHAR(255),
                is_active BOOLEAN DEFAULT TRUE,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            );

            -- Insert default roles
            INSERT INTO roles (name, description) VALUES 
            ('User', 'Standard user role'),
            ('Admin', 'Administrator role')
            ON CONFLICT (name) DO NOTHING;

            -- Insert test professions
            INSERT INTO professions (name, description) VALUES 
            ('Software Developer', 'Software development and programming'),
            ('Doctor', 'Medical professional'),
            ('Teacher', 'Education professional'),
            ('Engineer', 'Engineering professional'),
            ('Lawyer', 'Legal professional')
            ON CONFLICT DO NOTHING;
        ";

        using var command = _dbConnection.CreateCommand();
        command.CommandText = createTablesScript;
        await command.ExecuteNonQueryAsync();
    }
} 