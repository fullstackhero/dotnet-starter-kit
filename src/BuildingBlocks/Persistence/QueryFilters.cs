namespace FSH.Framework.Persistence;

/// <summary>
/// Stable names for the named EF Core query filters registered by the
/// framework. Reference these from call sites that want to surgically
/// disable a single filter (via <c>IgnoreQueryFilters([name])</c>) instead
/// of stripping every filter on the entity.
/// </summary>
public static class QueryFilters
{
    /// <summary>
    /// Hides rows where <c>ISoftDeletable.IsDeleted == true</c>. Disable
    /// this name on trash views and restore handlers; tenant scoping and
    /// any other filters on the entity remain in force.
    /// </summary>
    public const string SoftDelete = nameof(SoftDelete);
}
