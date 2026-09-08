using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// Resolves the provider-portable intent declared by <see cref="PortableColumnExtensions"/> and
/// <see cref="PortableIndexExtensions"/> into concrete, provider-specific model configuration.
/// </summary>
/// <remarks>
/// <para>
/// This is the single place in the framework that knows how the two supported providers differ at
/// the model level. Entity configurations declare what they want; this decides how to spell it.
/// </para>
/// <para>
/// Implemented as a model-finalizing convention rather than a call at the end of
/// <c>OnModelCreating</c> on purpose: it must observe every entity configuration no matter where a
/// subclass chooses to call <c>base.OnModelCreating</c>. A context that calls base first would
/// otherwise silently leave its intent unresolved, and the leftover annotations then fail the
/// migrations scaffolder.
/// </para>
/// </remarks>
public sealed class HeroProviderConventions : IModelFinalizingConvention
{
    // Npgsql's public builder API (HasMethod/HasOperators) needs an IndexBuilder, which is long gone
    // by the time conventions run. These are the annotation names those methods set.
    private const string NpgsqlIndexMethod = "Npgsql:IndexMethod";
    private const string NpgsqlIndexOperators = "Npgsql:IndexOperators";

    /// <summary>
    /// Length EF Core's SQL Server provider gives a string key with no explicit MaxLength — the
    /// widest nvarchar that still fits the 900-byte index key limit.
    /// </summary>
    private const int SqlServerDefaultKeyLength = 450;

    private readonly string? _provider;

    /// <param name="provider">
    /// A <see cref="DbProviders"/> constant, or null for a provider with no framework-specific
    /// conventions (SQLite, in-memory). A null provider still strips the intent annotations.
    /// </param>
    public HeroProviderConventions(string? provider) => _provider = DbProviderResolver.Normalize(provider);

    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (IConventionEntityType entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            ApplyColumnConventions(entityType);
            ApplyIndexConventions(entityType);

            if (_provider == DbProviders.MSSQL)
            {
                ApplyUtcDateTimeConventions(entityType);
                ApplyForeignKeyLengthConventions(entityType);
            }
        }
    }

    private void ApplyColumnConventions(IConventionEntityType entityType)
    {
        foreach (IConventionProperty property in entityType.GetProperties())
        {
            if (property.FindAnnotation(PortableColumnExtensions.ColumnKindAnnotation)?.Value is PortableColumnKind columnKind)
            {
                property.RemoveAnnotation(PortableColumnExtensions.ColumnKindAnnotation);

                string? columnType = ResolveColumnType(columnKind, _provider);
                if (columnType is not null)
                {
                    property.SetColumnType(columnType);
                }
            }

            if (property.FindAnnotation(PortableColumnExtensions.DefaultKindAnnotation)?.Value is PortableDefaultKind defaultKind)
            {
                property.RemoveAnnotation(PortableColumnExtensions.DefaultKindAnnotation);

                string? defaultSql = ResolveDefaultSql(defaultKind, _provider);
                if (defaultSql is not null)
                {
                    property.SetDefaultValueSql(defaultSql);
                }
            }
        }
    }

    private static string? ResolveColumnType(PortableColumnKind kind, string? provider) => (kind, provider) switch
    {
        (PortableColumnKind.Json, DbProviders.PostgreSQL) => "jsonb",

        // The native SQL Server json type, not nvarchar(max). Requires SQL Server 2025 (17.x) or
        // Azure SQL, and compatibility level 170 — see OptionsBuilderExtensions.
        (PortableColumnKind.Json, DbProviders.MSSQL) => "json",

        (PortableColumnKind.Text, DbProviders.PostgreSQL) => "text",
        (PortableColumnKind.Text, DbProviders.MSSQL) => "nvarchar(max)",

        // Unknown provider: leave EF's default mapping in place.
        _ => null
    };

    private static string? ResolveDefaultSql(PortableDefaultKind kind, string? provider) => (kind, provider) switch
    {
        (PortableDefaultKind.EmptyJson, DbProviders.PostgreSQL) => "'{}'::jsonb",
        (PortableDefaultKind.EmptyJson, DbProviders.MSSQL) => "N'{}'",

        (PortableDefaultKind.UtcNow, DbProviders.PostgreSQL) => "CURRENT_TIMESTAMP",
        (PortableDefaultKind.UtcNow, DbProviders.MSSQL) => "SYSUTCDATETIME()",

        _ => null
    };

    private void ApplyIndexConventions(IConventionEntityType entityType)
    {
        // Snapshot first: the search-index branch removes indexes from the entity.
        var indexes = entityType.GetIndexes().ToList();

        foreach (IConventionIndex index in indexes)
        {
            if (index.FindAnnotation(PortableIndexExtensions.IndexFilterAnnotation)?.Value is string encodedFilter)
            {
                index.RemoveAnnotation(PortableIndexExtensions.IndexFilterAnnotation);

                IReadOnlyList<PortableFilterTerm> terms = PortableFilterTerm.Decode(encodedFilter);
                if (_provider is not null && terms.Count > 0)
                {
                    index.SetFilter(ProviderFilterRenderer.Render(terms, _provider));
                }
            }

            if (index.FindAnnotation(PortableIndexExtensions.IndexKindAnnotation)?.Value is PortableIndexKind indexKind)
            {
                index.RemoveAnnotation(PortableIndexExtensions.IndexKindAnnotation);
                ApplySearchIndexKind(entityType, index, indexKind);
            }
        }
    }

    private void ApplySearchIndexKind(IConventionEntityType entityType, IConventionIndex index, PortableIndexKind kind)
    {
        if (_provider == DbProviders.PostgreSQL)
        {
            index.SetAnnotation(NpgsqlIndexMethod, "gin");
            index.SetAnnotation(
                NpgsqlIndexOperators,
                new[] { kind == PortableIndexKind.JsonContainment ? "jsonb_path_ops" : "gin_trgm_ops" });
            return;
        }

        if (_provider == DbProviders.MSSQL)
        {
            // Neither shape survives as a regular index on SQL Server:
            //  - JsonContainment targets a `json` column, which cannot carry a regular index at all.
            //    The migration issues CREATE JSON INDEX instead, deliberately outside the EF model.
            //  - TrigramSearch targets unbounded nvarchar(max) columns, which exceed the 1700-byte
            //    nonclustered key limit. A B-tree index would not serve a leading-wildcard LIKE
            //    anyway — that is exactly what makes PostgreSQL's trigram GIN index special — so
            //    dropping it costs nothing a plain index would have provided.
            entityType.RemoveIndex(index);
        }
    }

    /// <summary>
    /// Widens string foreign-key columns to match the length of the principal key they reference.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SQL Server refuses to create a foreign key whose columns differ in length from the principal's
    /// ("Columns participating in a foreign key relationship must be defined with the same length and
    /// scale", error 1753). PostgreSQL has no such rule — <c>text</c> and <c>varchar(n)</c> reference
    /// each other happily — so models written against it can carry mismatches that only surface here.
    /// </para>
    /// <para>
    /// Applied on SQL Server only, so the PostgreSQL schema is unaffected. It widens the dependent
    /// rather than narrowing the principal, which is the only direction that cannot lose data.
    /// </para>
    /// </remarks>
    private static void ApplyForeignKeyLengthConventions(IConventionEntityType entityType)
    {
        foreach (IConventionForeignKey foreignKey in entityType.GetForeignKeys())
        {
            IReadOnlyList<IConventionProperty> dependents = foreignKey.Properties;
            IReadOnlyList<IConventionProperty> principals = foreignKey.PrincipalKey.Properties;

            for (int i = 0; i < dependents.Count && i < principals.Count; i++)
            {
                IConventionProperty dependent = dependents[i];
                IConventionProperty principal = principals[i];

                if (dependent.ClrType != typeof(string) || principal.ClrType != typeof(string))
                {
                    continue;
                }

                // A string key with no explicit length becomes nvarchar(450) on SQL Server — the
                // widest value that still fits the 900-byte index key limit.
                int principalLength = principal.GetMaxLength() ?? SqlServerDefaultKeyLength;

                if (dependent.GetMaxLength() != principalLength)
                {
                    dependent.SetMaxLength(principalLength);
                }
            }
        }
    }

    private static void ApplyUtcDateTimeConventions(IConventionEntityType entityType)
    {
        foreach (IConventionProperty property in entityType.GetProperties())
        {
            Type propertyType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

            // Never stomp an explicitly configured converter.
            if (propertyType == typeof(DateTime) && property.GetValueConverter() is null)
            {
                property.SetValueConverter(UtcDateTimeConverter.Instance);
            }
        }
    }
}
