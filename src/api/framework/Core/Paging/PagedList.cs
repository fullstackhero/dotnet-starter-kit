using Mapster;

namespace FSH.Framework.Core.Paging;

public record PagedList<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount) : IPagedList<T>
    where T : class
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
    public IPagedList<TR> MapTo<TR>(Func<T, TR> map)
        where TR : class
    {
        return new PagedList<TR>(Items.Select(map).ToList(), PageNumber, PageSize, TotalCount);
    }
    public IPagedList<TR> MapTo<TR>()
        where TR : class
    {
        return new PagedList<TR>(Items.Adapt<IReadOnlyList<TR>>(), PageNumber, PageSize, TotalCount);
    }
}
