namespace FSH.Framework.Core.Paging;

public class Search
{
    public List<string> Fields { get; set; } = new();
    public string? Keyword { get; set; }
}
