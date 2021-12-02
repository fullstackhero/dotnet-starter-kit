using ProtoBuf;

namespace DN.WebApi.Application.Wrapper;

[ProtoContract]
public class PaginatedResult<T> : IResult
{
    public PaginatedResult(List<T> data)
    {
        Data = data;
    }

    [ProtoMember(1)]
    public List<T>? Data { get; set; }

    [ProtoMember(2)]
    public List<string>? Messages { get; set; } = new();

    [ProtoMember(3)]
    public bool Succeeded { get; set; }

    internal PaginatedResult(bool succeeded, List<T>? data = default, List<string>? messages = null, int count = 0, int page = 1, int pageSize = 10)
    {
        Data = data;
        CurrentPage = page;
        Succeeded = succeeded;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Messages = messages;
    }

    public PaginatedResult() { }

    public static PaginatedResult<T> Failure(List<string> messages)
    {
        return new(false, default, messages);
    }

    public static PaginatedResult<T> Success(List<T> data, int count, int page, int pageSize)
    {
        return new(true, data, null, count, page, pageSize);
    }

    [ProtoMember(4)]
    public int CurrentPage { get; set; }

    [ProtoMember(5)]
    public int TotalPages { get; set; }

    [ProtoMember(6)]
    public int TotalCount { get; set; }

    [ProtoMember(7)]
    public int PageSize { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;
}