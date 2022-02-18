namespace FSH.WebApi.Application.Common.Models;

public class PaginationResponse<T>
{
    public PaginationResponse(List<T> data, int count, int page, int pageSize)
    {
        Data = data;
        CurrentPage = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
    }

    public List<T> Data { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int PageSize { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;
}

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