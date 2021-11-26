namespace DN.WebApi.Shared.DTOs.Filters;

public class BaseFilter
{
    /// <summary>
    /// Column Wise Search is Supported.
    /// </summary>
    public Search AdvancedSearch { get; set; }

    /// <summary>
    /// Keyword to Search in All the available columns of the Resource.
    /// </summary>
    public string Keyword { get; set; }
}