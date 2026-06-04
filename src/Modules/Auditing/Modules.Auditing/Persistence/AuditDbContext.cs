using System.Linq.Expressions;
using System.Reflection;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Auditing.Persistence;

public sealed class AuditDbContext : BaseDbContext
{
    public AuditDbContext(
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    DbContextOptions<AuditDbContext> options,
    IOptions<DatabaseOptions> settings,
    IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Required for the trigram GIN indexes on Source / UserName. The
        // extension is idempotent (CREATE EXTENSION IF NOT EXISTS) — the
        // role running migrations needs CREATE permission on the database.
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);

        // Map AuditJsonbFunctions.AsText(string) to a SQL `CAST(x AS text)` so the
        // jsonb PayloadJson column can be searched with text operators (ILIKE).
        // Without the cast, ILIKE on a jsonb column throws at execution:
        // "function pg_catalog.like_escape(jsonb, unknown) does not exist" → HTTP 500.
        var textMapping = this.GetService<IRelationalTypeMappingSource>().FindMapping(typeof(string))!;
        var asTextMethod = typeof(AuditJsonbFunctions)
            .GetMethod(nameof(AuditJsonbFunctions.AsText), BindingFlags.Public | BindingFlags.Static)!;
        modelBuilder
            .HasDbFunction(asTextMethod)
            .HasTranslation(args => new SqlUnaryExpression(
                ExpressionType.Convert,
                args[0],
                typeof(string),
                textMapping));

        // base.OnModelCreating runs LAST so BaseDbContext's auto-apply sees
        // fully-configured entities (including HasMany child types).
        base.OnModelCreating(modelBuilder);
    }
}