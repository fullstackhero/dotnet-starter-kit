using System.Globalization;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// Comparison used by a term of a portable partial-index filter.
/// </summary>
public enum PortableFilterOperator
{
    /// <summary>Column equals the term's value.</summary>
    Equal,

    /// <summary>Column does not equal the term's value.</summary>
    NotEqual,

    /// <summary>Column IS NULL.</summary>
    IsNull,

    /// <summary>Column IS NOT NULL.</summary>
    IsNotNull
}

/// <summary>
/// How a term's literal should be rendered. The kind is carried separately from the value because
/// booleans have no portable spelling — PostgreSQL wants <c>TRUE</c>/<c>FALSE</c>, SQL Server wants
/// <c>1</c>/<c>0</c> against a <c>bit</c> column.
/// </summary>
public enum PortableFilterLiteralKind
{
    /// <summary>No literal — the operator is IS NULL / IS NOT NULL.</summary>
    None,

    /// <summary>An integer literal, rendered identically on both providers.</summary>
    Number,

    /// <summary>A boolean literal, rendered per provider.</summary>
    Boolean,

    /// <summary>
    /// The soft-delete boolean. Rendered as uppercase <c>FALSE</c> on PostgreSQL rather than going
    /// through <see cref="Boolean"/>. See <c>ProviderFilterRenderer</c> for why the two spellings
    /// must both be preserved.
    /// </summary>
    SoftDeleteBoolean
}

/// <summary>
/// One conjunct of a partial-index filter, expressed without provider-specific SQL.
/// Terms are ANDed together in the order they were declared.
/// </summary>
/// <param name="Column">The column name, unquoted.</param>
/// <param name="Operator">The comparison to apply.</param>
/// <param name="LiteralKind">How <paramref name="Value"/> should be rendered.</param>
/// <param name="Value">The literal value, or null for IS NULL / IS NOT NULL.</param>
public sealed record PortableFilterTerm(
    string Column,
    PortableFilterOperator Operator,
    PortableFilterLiteralKind LiteralKind,
    string? Value)
{
    // Safe because every field is an identifier, an enum ordinal or an integer literal — none of
    // which can contain either separator.
    private const char FieldSeparator = '|';
    private const char TermSeparator = ';';

    /// <summary>
    /// Encodes terms for storage in an EF annotation.
    /// </summary>
    /// <remarks>
    /// A string rather than the record itself: EF must be able to emit any surviving annotation as a
    /// C# literal when scaffolding a migration snapshot, and it can only do that for primitives. An
    /// annotation carrying a POCO turns a stray leftover into a hard
    /// "Cannot scaffold C# literals of type ..." failure instead of something harmless.
    /// </remarks>
    public static string Encode(IEnumerable<PortableFilterTerm> terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        return string.Join(TermSeparator, terms.Select(t => string.Join(
            FieldSeparator,
            t.Column,
            ((int)t.Operator).ToString(CultureInfo.InvariantCulture),
            ((int)t.LiteralKind).ToString(CultureInfo.InvariantCulture),
            t.Value ?? string.Empty)));
    }

    /// <summary>
    /// Decodes terms previously produced by <see cref="Encode"/>.
    /// </summary>
    public static IReadOnlyList<PortableFilterTerm> Decode(string encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);

        return encoded
            .Split(TermSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(raw =>
            {
                string[] parts = raw.Split(FieldSeparator);
                return new PortableFilterTerm(
                    parts[0],
                    (PortableFilterOperator)int.Parse(parts[1], CultureInfo.InvariantCulture),
                    (PortableFilterLiteralKind)int.Parse(parts[2], CultureInfo.InvariantCulture),
                    parts[3].Length == 0 ? null : parts[3]);
            })
            .ToList();
    }
}
