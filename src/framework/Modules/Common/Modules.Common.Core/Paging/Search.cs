namespace FSH.Framework.Core.Paging;

/// <summary>
/// Represents a search filter with target fields and a keyword.
/// </summary>
public class Search
{
    /// <summary>
    /// The list of field names to apply the search keyword against.
    /// </summary>
    public IReadOnlyList<string> Fields { get; set; } = new List<string>();

    /// <summary>
    /// The keyword to search for across the specified fields.
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// Returns true if both fields and keyword are provided.
    /// </summary>
    public bool IsValid =>
        Fields?.Count > 0 && !string.IsNullOrWhiteSpace(Keyword);
}