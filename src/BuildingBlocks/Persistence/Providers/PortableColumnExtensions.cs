using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// The provider-specific shape a column takes, declared as intent by entity configurations and
/// resolved to a concrete column type by <see cref="HeroProviderConventions"/>.
/// </summary>
public enum PortableColumnKind
{
    /// <summary>A JSON document: <c>jsonb</c> on PostgreSQL, the native <c>json</c> type on SQL Server.</summary>
    Json,

    /// <summary>Unbounded text: <c>text</c> on PostgreSQL, <c>nvarchar(max)</c> on SQL Server.</summary>
    Text
}

/// <summary>
/// The provider-specific default a column takes.
/// </summary>
public enum PortableDefaultKind
{
    /// <summary>An empty JSON object.</summary>
    EmptyJson,

    /// <summary>The current UTC timestamp, evaluated by the database.</summary>
    UtcNow
}

/// <summary>
/// Declares provider-portable column intent on a property. These write annotations only — the
/// concrete column type is applied by <see cref="HeroProviderConventions"/>
/// once the target provider is known.
/// </summary>
/// <remarks>
/// Prefer these over <c>HasColumnType</c> with a literal type name. A literal locks the model to one
/// provider and fails at model-build time on the other.
/// </remarks>
public static class PortableColumnExtensions
{
    internal const string ColumnKindAnnotation = "Fsh:ColumnKind";
    internal const string DefaultKindAnnotation = "Fsh:DefaultKind";

    /// <summary>
    /// Maps the property to the provider's JSON document type.
    /// </summary>
    public static PropertyBuilder<TProperty> HasJsonColumn<TProperty>(this PropertyBuilder<TProperty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasAnnotation(ColumnKindAnnotation, PortableColumnKind.Json);
    }

    /// <summary>
    /// Maps the property to the provider's unbounded text type.
    /// </summary>
    public static PropertyBuilder<TProperty> HasUnboundedTextColumn<TProperty>(this PropertyBuilder<TProperty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasAnnotation(ColumnKindAnnotation, PortableColumnKind.Text);
    }

    /// <summary>
    /// Defaults the column to an empty JSON object.
    /// </summary>
    public static PropertyBuilder<TProperty> HasJsonDefaultEmptyObject<TProperty>(this PropertyBuilder<TProperty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasAnnotation(DefaultKindAnnotation, PortableDefaultKind.EmptyJson);
    }

    /// <summary>
    /// Defaults the column to the database's current UTC timestamp.
    /// </summary>
    public static PropertyBuilder<TProperty> HasUtcNowDefault<TProperty>(this PropertyBuilder<TProperty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasAnnotation(DefaultKindAnnotation, PortableDefaultKind.UtcNow);
    }
}
