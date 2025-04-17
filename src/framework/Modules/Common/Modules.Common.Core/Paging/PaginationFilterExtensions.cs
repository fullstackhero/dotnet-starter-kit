namespace FSH.Framework.Core.Paging;

public static class PaginationFilterExtensions
{
    /// <summary>
    /// Returns true if the PaginationFilter contains any OrderBy fields.
    /// </summary>
    public static bool HasOrderBy(this PaginationFilter? filter) =>
        filter?.OrderBy?.Any() == true;
}