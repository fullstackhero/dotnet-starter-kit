namespace FSH.Framework.Core.Paging;

public interface IPagedList<T>
    where T : class
{
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
    IReadOnlyList<T> Items { get; init; }
    int TotalCount { get; init; }
    int PageNumber { get; init; }
    int PageSize { get; init; }

    IPagedList<TR> MapTo<TR>(Func<T, TR> map)
        where TR : class;
    IPagedList<TR> MapTo<TR>()
       where TR : class;
}
