using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FSH.Starter.Tests.Unit")]

namespace FSH.Starter.WebApi.Contracts.Common;

internal class ApiResponse<T>
{
    protected ApiResponse(bool success, string message, T? data = default, IReadOnlyCollection<string>? errors = null)
    {
        Success = success;
        Message = message;
        Data = data;
        Errors = errors;
    }

    public bool Success { get; }
    public string Message { get; }
    public T? Data { get; }
    public IReadOnlyCollection<string>? Errors { get; }

    public static ApiResponse<T> CreateSuccess(T data, string message = "Operation successful") =>
        new ApiResponse<T>(true, message, data);

    public static ApiResponse<T> CreateFailure(string message, IReadOnlyCollection<string>? errors = null) =>
        new ApiResponse<T>(false, message, default, errors);

    public static ApiResponse<T> CreateFailure(string message, string error) =>
        new ApiResponse<T>(false, message, default, new ReadOnlyCollection<string>(new[] { error }));

    // Enterprise-friendly static shortcuts for controller usage
    public static ApiResponse<T> SuccessResult(T data, string message = "Operation successful") =>
        CreateSuccess(data, message);

    public static ApiResponse<T> FailureResult(string message, IReadOnlyCollection<string>? errors = null) =>
        CreateFailure(message, errors);

    public static ApiResponse<T> FailureResult(string message, string error) =>
        CreateFailure(message, error);
}

internal sealed class ApiResponse : ApiResponse<object?>
{
    private ApiResponse(bool success, string message, object? data = null, IReadOnlyCollection<string>? errors = null)
        : base(success, message, data, errors)
    {
    }

    public static ApiResponse CreateSuccess(string message = "Operation successful") =>
        new ApiResponse(true, message);

    public static new ApiResponse CreateFailure(string message, IReadOnlyCollection<string>? errors = null) =>
        new ApiResponse(false, message, null, errors);

    public static new ApiResponse CreateFailure(string message, string error) =>
        new ApiResponse(false, message, null, new ReadOnlyCollection<string>(new[] { error }));
}
