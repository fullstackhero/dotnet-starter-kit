using System.Globalization;
using System.Reflection;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Architecture.Tests;

/// <summary>
/// Catches an entity change that got a migration for one database provider but not the other.
/// </summary>
/// <remarks>
/// <para>
/// With one migrations project per provider, forgetting the second one fails nothing: the build
/// passes, the tests pass, the API starts. The forgotten provider just deploys against a stale
/// schema until something breaks in production.
/// </para>
/// <para>
/// The check is per-provider drift — the live model against <em>that provider's own</em> snapshot,
/// which is the in-process equivalent of <c>dotnet ef migrations has-pending-model-changes</c>.
/// Comparing the two snapshots against each other would not work: the models differ legitimately
/// (SQL Server adds the <c>IdSort</c> shadow property and drops the trigram/JSON indexes), and the
/// migration histories are not comparable at all (51 historical PostgreSQL migrations vs 13
/// consolidated MSSQL ones).
/// </para>
/// <para>
/// Maintaining both providers is <b>not</b> mandatory. A provider listed in
/// <c>FshMaintainedDbProviders</c> (see <c>src/Directory.Build.props</c>) fails the build when it
/// drifts; any other provider only reports it. That way a project that has settled on one engine is
/// never blocked by migrations for an engine it does not use.
/// </para>
/// </remarks>
public class MigrationDriftTests(ITestOutputHelper output)
{
    private const string MaintainedProvidersMetadataKey = "FshMaintainedDbProviders";
    private const string MaintainedProvidersEnvironmentVariable = "FSH_MIGRATIONS_PROVIDERS";

    private static readonly string[] AllProviders = [DbProviders.PostgreSQL, DbProviders.MSSQL];

    [Fact]
    public void Maintained_Providers_Should_Have_No_Pending_Model_Changes()
    {
        var maintained = MaintainedProviders();
        var violations = new List<string>();

        foreach (string provider in maintained)
        {
            violations.AddRange(DriftFor(provider));
        }

        violations.ShouldBeEmpty(
            "A model change is missing its migration. Generate it for the provider(s) below, or narrow "
            + $"FshMaintainedDbProviders in src/Directory.Build.props if this project no longer keeps that "
            + $"engine up to date.\n  {string.Join("\n  ", violations)}");
    }

    [Fact]
    public void Unmaintained_Providers_Should_Report_Drift_Without_Failing()
    {
        var maintained = MaintainedProviders();
        var advisory = AllProviders.Except(maintained, StringComparer.Ordinal).ToList();

        // The invariant that makes this test meaningful: a provider is either enforced by
        // Maintained_Providers_Should_Have_No_Pending_Model_Changes or reported here, never both and
        // never neither — otherwise a provider could silently escape the guard entirely.
        advisory.Concat(maintained).OrderBy(p => p, StringComparer.Ordinal)
            .ShouldBe(AllProviders.OrderBy(p => p, StringComparer.Ordinal));

        var reported = new List<string>();

        foreach (string provider in advisory)
        {
            foreach (string drift in DriftFor(provider))
            {
                // Advisory on purpose: this provider is not maintained here, so a stale migration
                // must not break the build.
                output.WriteLine($"[migration-drift] {drift}");
                reported.Add(drift);
            }
        }

        Report(reported);
    }

    /// <summary>
    /// Publishes advisory drift where someone will actually see it.
    /// </summary>
    /// <remarks>
    /// <see cref="ITestOutputHelper"/> alone is not enough: for a passing test it only shows under
    /// <c>--logger "console;verbosity=detailed"</c>, which CI does not use, so the warning would be
    /// invisible exactly where it matters. The job summary needs no change to the workflow.
    /// </remarks>
    private static void Report(List<string> drift)
    {
        if (drift.Count == 0)
        {
            return;
        }

        string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrWhiteSpace(summaryPath))
        {
            return;
        }

        var summary = new System.Text.StringBuilder()
            .AppendLine("### ⚠️ Migration drift on an unmaintained provider")
            .AppendLine()
            .AppendLine("These migrations are behind the model. Not fatal — this provider is not listed in")
            .AppendLine("`FshMaintainedDbProviders` — but the schema will be stale if you ever deploy it.")
            .AppendLine();

        foreach (string item in drift)
        {
            summary.Append("- ").AppendLine(item);
        }

        File.AppendAllText(summaryPath, summary.ToString());
    }

    [Fact]
    public void Every_Context_Should_Have_A_Snapshot_In_Both_Providers()
    {
        var postgres = SnapshotContextNames(DbProviders.PostgreSQL);
        var mssql = SnapshotContextNames(DbProviders.MSSQL);

        var missingInMssql = postgres.Except(mssql, StringComparer.Ordinal).ToList();
        var missingInPostgres = mssql.Except(postgres, StringComparer.Ordinal).ToList();

        var gaps = missingInMssql.Select(c => $"{c}: no snapshot in the MSSQL migrations project")
            .Concat(missingInPostgres.Select(c => $"{c}: no snapshot in the PostgreSQL migrations project"))
            .ToList();

        if (gaps.Count == 0)
        {
            return;
        }

        // Only a hard failure while both engines are maintained. With one engine, a context having
        // migrations for that engine alone is the expected state, not a defect.
        if (MaintainedProviders().Count == AllProviders.Length)
        {
            gaps.ShouldBeEmpty(
                "A DbContext has migrations for only one provider. A new module needs a folder in BOTH "
                + $"migrations projects.\n  {string.Join("\n  ", gaps)}");
        }

        foreach (string gap in gaps)
        {
            output.WriteLine($"[migration-drift] {gap}");
        }
    }

    [Fact]
    public void The_Guard_Should_Actually_See_Both_Providers_And_Every_Context()
    {
        // Without this the tests above pass vacuously the moment assembly discovery or the snapshot
        // reflection stops finding anything — which is exactly when they are most needed.
        foreach (string provider in AllProviders)
        {
            var contexts = SnapshotContextTypes(provider);

            contexts.Count.ShouldBeGreaterThanOrEqualTo(
                11,
                $"expected at least 11 DbContext snapshots in {ProviderDbContextFactory.MigrationsAssemblyFor(provider)}, "
                + $"found {contexts.Count}");
        }

        // The three contexts that do not derive from BaseDbContext are the ones most likely to fall
        // out of a reflection-based sweep, so assert them by name.
        var names = SnapshotContextNames(DbProviders.PostgreSQL);
        names.ShouldContain("IdentityDbContext");
        names.ShouldContain("TenantDbContext");
        names.ShouldContain("BillingDbContext");
    }

    /// <summary>
    /// Reports, per context, whether <paramref name="provider"/>'s migrations are behind the model.
    /// </summary>
    private static List<string> DriftFor(string provider)
    {
        var drift = new List<string>();
        string migrationsAssembly = ProviderDbContextFactory.MigrationsAssemblyFor(provider);

        foreach (Type contextType in SnapshotContextTypes(provider))
        {
            using DbContext context = ProviderDbContextFactory.Create(contextType, provider);

            var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot;
            if (snapshot is null)
            {
                drift.Add(
                    $"{contextType.Name} [{provider}]: no ModelSnapshot found in {migrationsAssembly}.");
                continue;
            }

            if (context.Database.HasPendingModelChanges())
            {
                drift.Add(
                    $"{contextType.Name} [{provider}]: model has changes with no migration. Run: "
                    + MigrationCommandFor(contextType, provider));
            }
        }

        return drift;
    }

    private static string MigrationCommandFor(Type contextType, string provider)
    {
        string migrationsAssembly = ProviderDbContextFactory.MigrationsAssemblyFor(provider);
        string project = provider == DbProviders.MSSQL
            ? "src/Host/FSH.Starter.Migrations.MSSQL"
            : "src/Host/FSH.Starter.Migrations.PostgreSQL";

        // MSSQL scaffolding needs the env vars, or the design-time model is built for PostgreSQL and
        // PostgreSQL DDL lands in the MSSQL project.
        string prefix = provider == DbProviders.MSSQL
            ? "DatabaseOptions__Provider=MSSQL "
              + $"DatabaseOptions__MigrationsAssembly={migrationsAssembly} "
              + "DatabaseOptions__ConnectionString='Server=localhost,1433;Database=fsh;User Id=sa;Password=…;TrustServerCertificate=True' "
            : string.Empty;

        return $"{prefix}dotnet ef migrations add <Name> --project {project} "
            + $"--startup-project src/Host/FSH.Starter.Api --context {contextType.Name} --output-dir <Folder>";
    }

    /// <summary>
    /// The DbContext types that have a snapshot in <paramref name="provider"/>'s migrations assembly.
    /// </summary>
    /// <remarks>
    /// Snapshots are the authoritative answer to "which context has migrations for which provider",
    /// and each carries <c>[DbContext(typeof(X))]</c>. Enumerating them also covers
    /// <c>EventingDbContext</c>, which lives in a framework assembly that
    /// <see cref="ModuleAssemblyDiscovery"/>'s <c>FSH.Modules.*</c> sweep does not reach.
    /// </remarks>
    private static List<Type> SnapshotContextTypes(string provider)
    {
        Assembly assembly = LoadMigrationsAssembly(provider);

        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ModelSnapshot).IsAssignableFrom(t))
            .Select(t => t.GetCustomAttribute<DbContextAttribute>()?.ContextType)
            .Where(t => t is not null)
            .Select(t => t!)
            .DistinctBy(t => t.FullName, StringComparer.Ordinal)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> SnapshotContextNames(string provider) =>
        SnapshotContextTypes(provider).Select(t => t.Name).ToList();

    private static Assembly LoadMigrationsAssembly(string provider)
    {
        string name = ProviderDbContextFactory.MigrationsAssemblyFor(provider);
        string path = Path.Combine(AppContext.BaseDirectory, $"{name}.dll");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"{name}.dll is not in the test output. Architecture.Tests must reference both migrations "
                + "projects for the migration guard to work.");
        }

        return Assembly.Load(AssemblyName.GetAssemblyName(path));
    }

    /// <summary>
    /// Which providers this project keeps migrations for: env var, then the build-time knob, then both.
    /// </summary>
    private static List<string> MaintainedProviders()
    {
        string? configured = Environment.GetEnvironmentVariable(MaintainedProvidersEnvironmentVariable);

        configured ??= typeof(MigrationDriftTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(a.Key, MaintainedProvidersMetadataKey, StringComparison.Ordinal))
            ?.Value;

        if (string.IsNullOrWhiteSpace(configured) || configured.Equals("both", StringComparison.OrdinalIgnoreCase))
        {
            return [.. AllProviders];
        }

        var selected = configured
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.ToUpperInvariant())
            .Where(p => AllProviders.Contains(p, StringComparer.Ordinal))
            .ToList();

        // An unrecognised value must not silently disable the guard.
        return selected.Count > 0
            ? selected
            : throw new InvalidOperationException(string.Create(
                CultureInfo.InvariantCulture,
                $"'{configured}' is not a valid {MaintainedProvidersMetadataKey} value. Use 'both', "
                + $"'{DbProviders.PostgreSQL}', '{DbProviders.MSSQL}', or a comma-separated list."));
    }
}
