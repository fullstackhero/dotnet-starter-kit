using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FSH.Framework.Persistence;

/// <summary>
/// Extension methods for converting IQueryable results to paginated responses.
/// </summary>
public static class PaginationExtensions
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    /// <summary>
    /// Converts an IQueryable to a paged response with the specified pagination parameters.
    /// </summary>
    /// <typeparam name="T">The type of items in the query.</typeparam>
    /// <param name="source">The queryable source to paginate.</param>
    /// <param name="pagination">The pagination parameters including page number and page size.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paged response containing the requested page of data and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source or pagination is null.</exception>
    public static Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> source,
        IPagedQuery pagination,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pagination);

        var pageNumber = pagination.PageNumber is null or <= 0
            ? 1
            : pagination.PageNumber.Value;

        var pageSize = pagination.PageSize is null or <= 0
            ? DefaultPageSize
            : pagination.PageSize.Value;

        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        // Decoupled from specifications: the source is expected to already have any required
        // ordering applied via specifications or explicit ordering at call sites.
        return ToPagedResponseInternalAsync(source, pageNumber, pageSize, cancellationToken);
    }

    /// <summary>
    /// Computes the zero-based row offset for a 1-based page number without <see cref="int"/> overflow.
    /// Page numbers below 1 are treated as page 1; an offset beyond <see cref="int.MaxValue"/> is clamped to it,
    /// which yields an empty page instead of wrapping into a negative OFFSET the database rejects.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The page size (values below 1 yield an offset of 0).</param>
    /// <returns>The number of rows to skip, always in the range [0, <see cref="int.MaxValue"/>].</returns>
    public static int GetOffset(int pageNumber, int pageSize)
    {
        if (pageNumber <= 1 || pageSize <= 0)
        {
            return 0;
        }

        long offset = (pageNumber - 1L) * pageSize;
        return offset > int.MaxValue ? int.MaxValue : (int)offset;
    }

    private static async Task<PagedResponse<T>> ToPagedResponseInternalAsync<T>(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
        where T : class
    {
        var totalCount = await source.LongCountAsync(cancellationToken).ConfigureAwait(false);

        // Nothing to page through: skip the item query and report page 1 of 0, so an out-of-range
        // page number on an empty listing never reaches the OFFSET arithmetic (#1416).
        if (totalCount == 0)
        {
            return EmptyPage<T>(pageNumber: 1, pageSize, totalCount: 0, totalPages: 0);
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        if (pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }

        // Clamping keeps this in range for any real table; the long math is a defensive backstop.
        long skip = (pageNumber - 1L) * pageSize;
        if (skip > int.MaxValue)
        {
            return EmptyPage<T>(pageNumber, pageSize, totalCount, totalPages);
        }

        var items = await source
            .Skip((int)skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static PagedResponse<T> EmptyPage<T>(int pageNumber, int pageSize, long totalCount, int totalPages)
        => new()
        {
            Items = Array.Empty<T>(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
}