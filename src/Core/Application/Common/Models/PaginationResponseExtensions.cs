namespace FSH.WebApi.Application.Common.Models;

public static class PaginationResponseExtensions
{
    public static async Task<PaginationResponse<TDestination>> PaginatedListAsync<T, TDestination>(
        this IReadRepository<T> repository, ISpecification<T, TDestination> spec, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        where T : class, IAggregateRoot
        where TDestination : class, IDto
    {
        var list = await repository.ListAsync(spec, cancellationToken);
        int count = await repository.CountAsync(spec, cancellationToken);

        return new PaginationResponse<TDestination>(list, count, pageNumber, pageSize);
    }
}