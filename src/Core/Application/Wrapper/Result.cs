using System.Collections.Generic;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Wrapper
{
    public class Result : IResult
    {
        public Result()
        {
        }

        public List<string> Messages { get; set; } = new();

        public bool Succeeded { get; set; }

        public static IResult Fail()
        {
            return new Result { Succeeded = false };
        }

        public static IResult Fail(string message)
        {
            return new Result { Succeeded = false, Messages = new List<string> { message } };
        }

        public static IResult Fail(List<string> messages)
        {
            return new Result { Succeeded = false, Messages = messages };
        }

        public static Task<IResult> FailAsync()
        {
            return Task.FromResult(Fail());
        }

        public static Task<IResult> FailAsync(string message)
        {
            return Task.FromResult(Fail(message));
        }

        public static Task<IResult> FailAsync(List<string> messages)
        {
            return Task.FromResult(Fail(messages));
        }

        public static IResult Success()
        {
            return new Result { Succeeded = true };
        }

        public static IResult Success(string message)
        {
            return new Result { Succeeded = true, Messages = new List<string> { message } };
        }

        public static IResult Success(List<string> messages)
        {
            return new Result { Succeeded = true, Messages = messages };
        }

        public static Task<IResult> SuccessAsync()
        {
            return Task.FromResult(Success());
        }

        public static Task<IResult> SuccessAsync(string message)
        {
            return Task.FromResult(Success(message));
        }

        public static Task<IResult> SuccessAsync(List<string> messages)
        {
            return Task.FromResult(Success(messages));
        }
    }

    public class ErrorResult<T> : Result<T>
    {
        public string Source { get; set; }

        public string Exception { get; set; }

        public int ErrorCode { get; set; }
        public string StackTrace { get; set; }
    }

    public class Result<T> : Result, IResult<T>
    {
        public Result()
        {
        }

        public T Data { get; set; }

        public static new Result<T> Fail()
        {
            return new() { Succeeded = false };
        }

        public static new Result<T> Fail(string message)
        {
            return new() { Succeeded = false, Messages = new List<string> { message } };
        }

        public static ErrorResult<T> ReturnError(string message)
        {
            return new() { Succeeded = false, Messages = new List<string> { message }, ErrorCode = 500 };
        }

        public static new Result<T> Fail(List<string> messages)
        {
            return new() { Succeeded = false, Messages = messages };
        }

        public static ErrorResult<T> ReturnError(List<string> messages)
        {
            return new() { Succeeded = false, Messages = messages, ErrorCode = 500 };
        }

        public static new Task<Result<T>> FailAsync()
        {
            return Task.FromResult(Fail());
        }

        public static new Task<Result<T>> FailAsync(string message)
        {
            return Task.FromResult(Fail(message));
        }

        public static Task<ErrorResult<T>> ReturnErrorAsync(string message)
        {
            return Task.FromResult(ReturnError(message));
        }

        public static new Task<Result<T>> FailAsync(List<string> messages)
        {
            return Task.FromResult(Fail(messages));
        }

        public static Task<ErrorResult<T>> ReturnErrorAsync(List<string> messages)
        {
            return Task.FromResult(ReturnError(messages));
        }

        public static new Result<T> Success()
        {
            return new() { Succeeded = true };
        }

        public static new Result<T> Success(string message)
        {
            return new() { Succeeded = true, Messages = new List<string> { message } };
        }

        public static new Result<T> Success(List<string> messages)
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

        public static new Task<Result<T>> SuccessAsync()
        {
            return Task.FromResult(Success());
        }

        public static new Task<Result<T>> SuccessAsync(string message)
        {
            return Task.FromResult(Success(message));
        }

        public static new Task<Result<T>> SuccessAsync(List<string> messages)
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
}