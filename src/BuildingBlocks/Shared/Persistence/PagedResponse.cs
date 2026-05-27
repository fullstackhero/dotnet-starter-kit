namespace FSH.Framework.Shared.Persistence;

public sealed class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public long TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasNext => PageNumber < TotalPages;

    public bool HasPrevious => PageNumber > 1;
}