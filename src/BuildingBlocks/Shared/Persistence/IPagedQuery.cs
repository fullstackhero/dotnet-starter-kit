namespace FSH.Framework.Shared.Persistence;

/// <summary>
/// Shared pagination and sorting contract that can be implemented
/// or extended by module-specific request types.
/// </summary>
public interface IPagedQuery
{
    /// <summary>
    /// 1-based page number. Values less than 1 are normalized to 1.
    /// </summary>
    int? PageNumber { get; set; }

    /// <summary>
    /// Requested page size. Implementations may enforce caps.
    /// </summary>
    int? PageSize { get; set; }

    /// <summary>
    /// Multi-column sort expression, for example: "Name,-CreatedOn".
    /// "-" prefix indicates descending order.
    /// </summary>
    string? Sort { get; set; }
}