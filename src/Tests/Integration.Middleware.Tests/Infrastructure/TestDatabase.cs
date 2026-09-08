using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace Integration.Middleware.Tests.Infrastructure;

/// <summary>
/// Picks the database container and matching <c>DatabaseOptions</c> for the provider under test.
/// </summary>
/// <remarks>
/// <para>
/// Selected by the <c>FSH_TEST_DB_PROVIDER</c> environment variable, defaulting to PostgreSQL so an
/// unqualified <c>dotnet test</c> behaves exactly as it did before SQL Server support existed. Set
/// it to <c>MSSQL</c> to run the same suite against SQL Server.
/// </para>
/// <para>
/// The SQL Server image is pinned to 2025 because the model maps JSON columns to the native
/// <c>json</c> type, which does not exist on 2019/2022 — the migrations simply will not apply there.
/// </para>
/// </remarks>
public sealed class TestDatabase
{
    public const string ProviderEnvironmentVariable = "FSH_TEST_DB_PROVIDER";

    /// <summary>
    /// Points the suite at an already-running server instead of starting a container.
    /// </summary>
    /// <remarks>
    /// Useful for capabilities a throwaway container does not have — the official
    /// <c>mcr.microsoft.com/mssql/server</c> image ships without Full-Text Search, so the chat
    /// search tests only exercise the <c>LIKE</c> fallback against it. Point this at an instance
    /// with FTS installed to cover the <c>FREETEXTTABLE</c> path. Also lets CI reuse a service
    /// container rather than paying for a nested one.
    /// </remarks>
    public const string ConnectionEnvironmentVariable = "FSH_TEST_DB_CONNECTION";

    private const string PostgresProvider = "POSTGRESQL";
    private const string MssqlProvider = "MSSQL";

    private readonly IDatabaseContainer? _container;
    private readonly string? _externalConnectionString;

    private TestDatabase(
        IDatabaseContainer? container,
        string? externalConnectionString,
        string provider,
        string migrationsAssembly)
    {
        _container = container;
        _externalConnectionString = externalConnectionString;
        Provider = provider;
        MigrationsAssembly = migrationsAssembly;
    }

    /// <summary>The <c>DatabaseOptions:Provider</c> value for the selected provider.</summary>
    public string Provider { get; }

    /// <summary>The <c>DatabaseOptions:MigrationsAssembly</c> value for the selected provider.</summary>
    public string MigrationsAssembly { get; }

    /// <summary>True when the suite is running against PostgreSQL.</summary>
    public bool IsPostgres => Provider == PostgresProvider;

    /// <summary>The provider the suite is configured to run against, without starting a container.</summary>
    public static string SelectedProvider =>
        (Environment.GetEnvironmentVariable(ProviderEnvironmentVariable) ?? PostgresProvider).ToUpperInvariant();

    /// <summary>True when the suite is configured to run against PostgreSQL.</summary>
    public static bool SelectedProviderIsPostgres => SelectedProvider == PostgresProvider;

    /// <summary>An externally provided server, or null to start a container.</summary>
    private static string? ExternalConnectionString
    {
        get
        {
            string? value = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    /// <summary>
    /// Builds the container for the selected provider. Call <see cref="StartAsync"/> to run it.
    /// </summary>
    /// <param name="databaseName">Database to create inside the container.</param>
    public static TestDatabase Create(string databaseName)
    {
        string migrationsAssembly = SelectedProvider == MssqlProvider
            ? "FSH.Starter.Migrations.MSSQL"
            : "FSH.Starter.Migrations.PostgreSQL";

        if (ExternalConnectionString is { } external)
        {
            return new TestDatabase(null, external, SelectedProvider, migrationsAssembly);
        }

        if (SelectedProvider == MssqlProvider)
        {
            return new TestDatabase(
                new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
                    .WithAutoRemove(true)
                    .WithCleanUp(true)
                    .Build(),
                null,
                MssqlProvider,
                migrationsAssembly);
        }

        return new TestDatabase(
            new PostgreSqlBuilder("postgres:17-alpine")
                .WithDatabase(databaseName)
                .WithUsername("postgres")
                .WithPassword("integration_test_pwd")
                .WithAutoRemove(true)
                .WithCleanUp(true)
                .Build(),
            null,
            PostgresProvider,
            migrationsAssembly);
    }

    public Task StartAsync() => _container?.StartAsync() ?? Task.CompletedTask;

    public ValueTask DisposeAsync() => _container?.DisposeAsync() ?? ValueTask.CompletedTask;

    public string GetConnectionString() => _externalConnectionString ?? _container!.GetConnectionString();
}
