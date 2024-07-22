namespace FSH.Starter.Blazor.Client.Components.EntityTable;

public class PaginationResponse<T>
{
    public List<T> Items { get; set; } = default!;
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
