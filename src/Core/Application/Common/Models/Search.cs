namespace FL_CRMS_ERP_WEBAPI.Application.Common.Models;

public class Search
{
    public List<string> Fields { get; set; } = new();
    public string? Keyword { get; set; }
}