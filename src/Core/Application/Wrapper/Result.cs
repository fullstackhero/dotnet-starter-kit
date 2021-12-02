using ProtoBuf;

namespace DN.WebApi.Application.Wrapper;

[ProtoContract]
public class ErrorResult<T> : Result<T> //need to be review for grpc
{
    [ProtoMember(1)]
    public string? Source { get; set; }

    [ProtoMember(2)]
    public string? Exception { get; set; }

    [ProtoMember(3)]
    public string? ErrorId { get; set; }

    [ProtoMember(4)]
    public string? SupportMessage { get; set; }

    [ProtoMember(5)]
    public int StatusCode { get; set; }
}

[ProtoContract]
public class Result<T> : IResult<T>
{
    [ProtoMember(1)]
    public T? Data { get; set; }

    [ProtoMember(2)]
    public List<string>? Messages { get; set; } = new();

    [ProtoMember(3)]
    public bool Succeeded { get; set; }

    public new static Result<T> Fail()
    {
        return new() { Succeeded = false };
    }

    public new static Result<T> Fail(string message)
    {
        return new() { Succeeded = false, Messages = new List<string> { message } };
    }

    public static ErrorResult<T> ReturnError(string message)
    {
        return new() { Succeeded = false, Messages = new List<string> { message }, StatusCode = 500 };
    }

    public new static Result<T> Fail(List<string> messages)
    {
        return new() { Succeeded = false, Messages = messages };
    }

    public static ErrorResult<T> ReturnError(List<string> messages)
    {
        return new() { Succeeded = false, Messages = messages, StatusCode = 500 };
    }

    public new static Task<Result<T>> FailAsync()
    {
        return Task.FromResult(Fail());
    }

    public new static Task<Result<T>> FailAsync(string message)
    {
        return Task.FromResult(Fail(message));
    }

    public static Task<ErrorResult<T>> ReturnErrorAsync(string message)
    {
        return Task.FromResult(ReturnError(message));
    }

    public new static Task<Result<T>> FailAsync(List<string> messages)
    {
        return Task.FromResult(Fail(messages));
    }

    public static Task<ErrorResult<T>> ReturnErrorAsync(List<string> messages)
    {
        return Task.FromResult(ReturnError(messages));
    }

    public new static Result<T> Success()
    {
        return new() { Succeeded = true };
    }

    public new static Result<T> Success(string message)
    {
        return new() { Succeeded = true, Messages = new List<string> { message } };
    }

    public new static Result<T> Success(List<string> messages)
    {
        return new() { Succeeded = true, Messages = messages };
    }

    public static Result<T> Success(T data)
    {
        return new() { Succeeded = true, Data = data };
    }

    public static Result<T> Success(T data, string message)
    {
        return new() { Succeeded = true, Data = data, Messages = new List<string> { message } };
    }

    public static Result<T> Success(T data, List<string> messages)
    {
        return new() { Succeeded = true, Data = data, Messages = messages };
    }

    public new static Task<Result<T>> SuccessAsync()
    {
        return Task.FromResult(Success());
    }

    public new static Task<Result<T>> SuccessAsync(string message)
    {
        return Task.FromResult(Success(message));
    }

    public new static Task<Result<T>> SuccessAsync(List<string> messages)
    {
        return Task.FromResult(Success(messages));
    }

    public static Task<Result<T>> SuccessAsync(T data)
    {
        return Task.FromResult(Success(data));
    }

    public static Task<Result<T>> SuccessAsync(T data, string message)
    {
        return Task.FromResult(Success(data, message));
    }

    public static Task<Result<T>> SuccessAsync(T data, List<string> messages)
    {
        return Task.FromResult(Success(data, messages));
    }
}