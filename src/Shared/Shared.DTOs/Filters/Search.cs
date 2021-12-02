using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Filters;

[DataContract]
public class Search
{
    [DataMember(Order = 1)]
    public List<string> Fields { get; set; } = new();

    [DataMember(Order = 2)]
    public string? Keyword { get; set; }
}