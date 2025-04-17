using FSH.Framework.Core.Paging;
using Mapster;

namespace FSH.Framework.Application.Extensions;

public static class PagedListExtensions
{
    public static PagedList<TR> AdaptPagedList<T, TR>(this PagedList<T> paged)
        where T : class
        where TR : class =>
        new(paged.Items.Adapt<IReadOnlyList<TR>>(), paged.PageNumber, paged.PageSize, paged.TotalCount);
}