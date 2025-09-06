namespace FSH.Framework.Core.Paging;

public interface IPagedList<out T>
{
    IReadOnlyList<T> Items { get; }
    int PageNumber { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}