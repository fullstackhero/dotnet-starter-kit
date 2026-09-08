using System.Globalization;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// A search-oriented index shape that has no portable spelling.
/// </summary>
public enum PortableIndexKind
{
    /// <summary>
    /// Substring search over a bounded text column. A trigram GIN index on PostgreSQL; a plain
    /// nonclustered index on SQL Server, which has no trigram equivalent.
    /// </summary>
    TrigramSearch,

    /// <summary>
    /// Containment search over a JSON column. A <c>jsonb_path_ops</c> GIN index on PostgreSQL. On
    /// SQL Server the index is removed from the EF model — a regular index over a <c>json</c>
    /// column is illegal — and the equivalent <c>CREATE JSON INDEX</c> is emitted as raw SQL by the
    /// migration instead.
    /// </summary>
    JsonContainment
}

/// <summary>
/// Declares provider-portable index intent. These write annotations only; the concrete filter SQL,
/// index method and operator class are applied by
/// <see cref="HeroProviderConventions"/>.
/// </summary>
/// <remarks>
/// Prefer these over <c>HasFilter</c> with a literal predicate. A literal bakes in one provider's
/// identifier quoting and boolean spelling, neither of which is portable.
/// </remarks>
public static class PortableIndexExtensions
{
    internal const string IndexFilterAnnotation = "Fsh:IndexFilter";
    internal const string IndexKindAnnotation = "Fsh:IndexKind";

    /// <summary>
    /// Restricts the index to rows that are not soft-deleted.
    /// </summary>
    public static IndexBuilder<TEntity> HasNotDeletedFilter<TEntity>(this IndexBuilder<TEntity> builder)
        => builder.AppendTerm(new PortableFilterTerm(
            "IsDeleted", PortableFilterOperator.Equal, PortableFilterLiteralKind.SoftDeleteBoolean, "false"));

    /// <summary>
    /// Restricts the index to rows where <paramref name="column"/> equals <paramref name="value"/>.
    /// </summary>
    public static IndexBuilder<TEntity> HasBoolFilter<TEntity>(this IndexBuilder<TEntity> builder, string column, bool value)
        => builder.AppendTerm(new PortableFilterTerm(
            column, PortableFilterOperator.Equal, PortableFilterLiteralKind.Boolean,
            value ? "true" : "false"));

    /// <summary>
    /// Restricts the index to rows where <paramref name="column"/> is not null.
    /// </summary>
    public static IndexBuilder<TEntity> HasNotNullFilter<TEntity>(this IndexBuilder<TEntity> builder, string column)
        => builder.AppendTerm(new PortableFilterTerm(
            column, PortableFilterOperator.IsNotNull, PortableFilterLiteralKind.None, null));

    /// <summary>
    /// Restricts the index to rows where <paramref name="column"/> is null.
    /// </summary>
    public static IndexBuilder<TEntity> HasNullFilter<TEntity>(this IndexBuilder<TEntity> builder, string column)
        => builder.AppendTerm(new PortableFilterTerm(
            column, PortableFilterOperator.IsNull, PortableFilterLiteralKind.None, null));

    /// <summary>
    /// Restricts the index to rows where <paramref name="column"/> equals <paramref name="value"/>.
    /// Use for enum discriminators stored as integers.
    /// </summary>
    public static IndexBuilder<TEntity> HasEqualsFilter<TEntity>(this IndexBuilder<TEntity> builder, string column, int value)
        => builder.AppendTerm(new PortableFilterTerm(
            column, PortableFilterOperator.Equal, PortableFilterLiteralKind.Number,
            value.ToString(CultureInfo.InvariantCulture)));

    /// <summary>
    /// Restricts the index to rows where <paramref name="column"/> does not equal <paramref name="value"/>.
    /// </summary>
    public static IndexBuilder<TEntity> HasNotEqualsFilter<TEntity>(this IndexBuilder<TEntity> builder, string column, int value)
        => builder.AppendTerm(new PortableFilterTerm(
            column, PortableFilterOperator.NotEqual, PortableFilterLiteralKind.Number,
            value.ToString(CultureInfo.InvariantCulture)));

    /// <summary>
    /// Marks the index as serving substring search over a text column.
    /// </summary>
    public static IndexBuilder<TEntity> AsTrigramSearchIndex<TEntity>(this IndexBuilder<TEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasAnnotation(IndexKindAnnotation, PortableIndexKind.TrigramSearch);
    }

    /// <summary>
    /// Marks the index as serving containment search over a JSON column.
    /// </summary>
    public static IndexBuilder<TEntity> AsJsonContainmentIndex<TEntity>(this IndexBuilder<TEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasAnnotation(IndexKindAnnotation, PortableIndexKind.JsonContainment);
    }

    private static IndexBuilder<TEntity> AppendTerm<TEntity>(this IndexBuilder<TEntity> builder, PortableFilterTerm term)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var terms = builder.Metadata.FindAnnotation(IndexFilterAnnotation)?.Value is string existing
            ? new List<PortableFilterTerm>(PortableFilterTerm.Decode(existing))
            : new List<PortableFilterTerm>(1);
        terms.Add(term);

        return builder.HasAnnotation(IndexFilterAnnotation, PortableFilterTerm.Encode(terms));
    }
}
