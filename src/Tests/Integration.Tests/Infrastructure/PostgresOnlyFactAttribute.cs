using Xunit;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// A fact that runs only when the suite is targeting PostgreSQL.
/// </summary>
/// <remarks>
/// For tests that assert PostgreSQL-specific behaviour rather than application behaviour — the
/// <c>FOR UPDATE SKIP LOCKED</c> outbox claim, and the canonical <c>jsonb::text</c> rendering the
/// audit payload filters match against. The SQL Server equivalents are covered by their own tests;
/// see <see cref="TestDatabase.ProviderEnvironmentVariable"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PostgresOnlyFactAttribute : FactAttribute
{
    public PostgresOnlyFactAttribute()
    {
        if (!TestDatabase.SelectedProviderIsPostgres)
        {
            Skip = $"PostgreSQL-specific behaviour; suite is running against {TestDatabase.SelectedProvider}.";
        }
    }
}

/// <summary>
/// A fact that runs only when the suite is targeting SQL Server.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SqlServerOnlyFactAttribute : FactAttribute
{
    public SqlServerOnlyFactAttribute()
    {
        if (TestDatabase.SelectedProviderIsPostgres)
        {
            Skip = $"SQL Server-specific behaviour; suite is running against {TestDatabase.SelectedProvider}.";
        }
    }
}
