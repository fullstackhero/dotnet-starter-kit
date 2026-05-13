namespace FSH.Starter.DbMigrator;

/// <summary>
/// Lightweight command-line parser. Avoids dragging in System.CommandLine for
/// a handful of flags — keep this honest and minimal.
///
/// Verbs:   apply | seed | list-pending  (default: apply)
/// Flags:   --tenant &lt;id&gt;   scope to one tenant id
///          --catalog-only   skip per-tenant migrations
///          --seed           after apply, also run SeedAsync per tenant
///          --help / -h      print help text
/// </summary>
internal sealed record MigratorCommand(
    string Command,
    string? Tenant,
    bool CatalogOnly,
    bool SeedAfter,
    bool Help)
{
    private static readonly string[] KnownVerbs = ["apply", "seed", "list-pending"];

    public static MigratorCommand Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var rawVerb = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "apply";
        // Canonicalise to one of the known verbs via case-insensitive match.
        // Using OrdinalIgnoreCase here (CA1308 forbids ToLowerInvariant for
        // security-sensitive normalisation) keeps the lookup explicit.
        var verb = KnownVerbs.FirstOrDefault(v => string.Equals(v, rawVerb, StringComparison.OrdinalIgnoreCase))
            ?? rawVerb;

        var tenant = ExtractValue(args, "--tenant");
        var catalogOnly = args.Any(a => string.Equals(a, "--catalog-only", StringComparison.OrdinalIgnoreCase));
        var seedAfter = args.Any(a => string.Equals(a, "--seed", StringComparison.OrdinalIgnoreCase));
        var help = args.Any(a => a is "-h" or "--help");

        return new MigratorCommand(verb, tenant, catalogOnly, seedAfter, help);
    }

    private static string? ExtractValue(string[] args, string flag)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
            // Also accept --flag=value form.
            if (args[i].StartsWith($"{flag}=", StringComparison.OrdinalIgnoreCase))
            {
                return args[i][(flag.Length + 1)..];
            }
        }
        return null;
    }

    public const string HelpText = """
        FSH DbMigrator — apply EF Core migrations across the tenant catalog
        and every tenant's per-module databases.

        Usage:
          dotnet run --project src/Host/FSH.Starter.DbMigrator -- [verb] [options]

        Verbs:
          apply           Apply pending migrations (default). Use --seed to also run SeedAsync.
          seed            Run only the SeedAsync step per tenant.
          list-pending    Print pending migrations without applying anything.

        Options:
          --tenant <id>        Restrict to a single tenant id (default: all tenants).
          --catalog-only       Skip the per-tenant pass; only the tenant catalog is migrated.
          --seed               After apply, also call ITenantService.SeedTenantAsync.
          -h, --help           Print this help text.

        Exit codes:
          0 — success
          1 — failure (see logged exception)
        """;
}
