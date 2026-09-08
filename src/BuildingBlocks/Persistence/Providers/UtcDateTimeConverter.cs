using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FSH.Framework.Persistence.Providers;

/// <summary>
/// Forces <see cref="DateTimeKind.Utc"/> on values read back from the database.
/// </summary>
/// <remarks>
/// PostgreSQL's <c>timestamp with time zone</c> round-trips as <see cref="DateTimeKind.Utc"/>, but
/// SQL Server's <c>datetime2</c> carries no kind and comes back as
/// <see cref="DateTimeKind.Unspecified"/>. Without this converter every timestamp the API serializes
/// on SQL Server would lose its trailing <c>Z</c>, and both React clients would render it as local
/// time. Applied to SQL Server only, so the PostgreSQL model is unchanged.
/// </remarks>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>Shared instance — the converter is stateless.</summary>
    public static readonly UtcDateTimeConverter Instance = new();

    private UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
