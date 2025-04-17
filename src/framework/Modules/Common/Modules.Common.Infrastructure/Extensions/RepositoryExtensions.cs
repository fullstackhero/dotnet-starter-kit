using Ardalis.Specification;
using FSH.Framework.Core.Paging;

namespace FSH.Framework.Application.Extensions;
public static class RepositoryExtensions
{
    public static async Task<PagedList<TDestination>> PaginatedListAsync<T, TDestination>(
        this IReadRepositoryBase<T> repository,
        ISpecification<T, TDestination> spec,
        PaginationFilter filter,
        CancellationToken cancellationToken = default)
        where T : class
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(repository);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<TDestination>(items, filter.PageNumber, filter.PageSize, totalCount);
    }
}