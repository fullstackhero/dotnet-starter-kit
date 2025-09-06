namespace FSH.Framework.Core.Paging;

public record PagedList<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount
) : IPagedList<T> where T : class
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;

    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Maps the paged list to another paged list using a custom projection.
    /// </summary>
    public PagedList<TR> MapTo<TR>(Func<T, TR> mapper)
        where TR : class =>
        new(Items.Select(mapper).ToList(), PageNumber, PageSize, TotalCount);
}