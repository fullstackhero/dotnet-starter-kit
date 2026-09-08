using System.Globalization;
using System.Text;
using FSH.Framework.Shared.Persistence;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// Renders <see cref="PortableFilterTerm"/> conjunctions to provider-specific partial-index SQL.
/// </summary>
internal static class ProviderFilterRenderer
{
    public static string Render(IReadOnlyList<PortableFilterTerm> terms, string provider)
    {
        var sql = new StringBuilder();

        for (int i = 0; i < terms.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(" AND ");
            }

            PortableFilterTerm term = terms[i];
            sql.Append(QuoteIdentifier(term.Column, provider));

            switch (term.Operator)
            {
                case PortableFilterOperator.IsNull:
                    sql.Append(" IS NULL");
                    break;
                case PortableFilterOperator.IsNotNull:
                    sql.Append(" IS NOT NULL");
                    break;
                case PortableFilterOperator.Equal:
                    sql.Append(" = ").Append(RenderLiteral(term, provider));
                    break;
                case PortableFilterOperator.NotEqual:
                    sql.Append(" <> ").Append(RenderLiteral(term, provider));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported filter operator {term.Operator}.");
            }
        }

        return sql.ToString();
    }

    private static string QuoteIdentifier(string column, string provider) =>
        provider == DbProviders.MSSQL ? $"[{column}]" : $"\"{column}\"";

    private static string RenderLiteral(PortableFilterTerm term, string provider)
    {
        switch (term.LiteralKind)
        {
            case PortableFilterLiteralKind.Number:
                return int.Parse(term.Value!, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);

            case PortableFilterLiteralKind.Boolean:
            case PortableFilterLiteralKind.SoftDeleteBoolean:
                bool value = bool.Parse(term.Value!);

                if (provider == DbProviders.MSSQL)
                {
                    // SQL Server has no boolean literal; the column is a `bit`.
                    return value ? "1" : "0";
                }

                // PostgreSQL accepts either casing, but EF diffs the filter as an opaque string:
                // changing the spelling drops and recreates the index on every existing database.
                // The two spellings below are the ones already in the shipped migrations, so they
                // are preserved deliberately. Do not "normalize" them.
                if (term.LiteralKind == PortableFilterLiteralKind.SoftDeleteBoolean)
                {
                    return value ? "TRUE" : "FALSE";
                }

                return value ? "true" : "false";

            default:
                throw new InvalidOperationException(
                    $"Filter operator {term.Operator} requires a literal but none was supplied.");
        }
    }
}
