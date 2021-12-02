using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Filters;

[DataContract]
public class PaginationFilter
{
    public PaginationFilter()
    {
        PageSize = int.MaxValue;
    }

    [DataMember(Order = 1)]
    public int PageNumber { get; set; }

    [DataMember(Order = 2)]
    public int PageSize { get; set; }

    [DataMember(Order = 3)]
    public string[]? OrderBy { get; set; }

    /// <summary>
    /// Column Wise Search is Supported.
    /// </summary>
    [DataMember(Order = 4)]
    public Search? AdvancedSearch { get; set; }

    /// <summary>
    /// Keyword to Search in All the available columns of the Resource.
    /// </summary>
    [DataMember(Order = 5)]
    public string? Keyword { get; set; }
}