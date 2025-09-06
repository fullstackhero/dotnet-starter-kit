namespace FSH.Framework.Core.Paging;

/// <summary>
/// Represents a filter expression used for dynamic querying.
/// </summary>
public class Filter
{
    public FilterLogic? Logic { get; set; }

    public IEnumerable<Filter>? Filters { get; set; }

    public string? Field { get; set; }

    public FilterOperator? Operator { get; set; }

    public object? Value { get; set; }
}