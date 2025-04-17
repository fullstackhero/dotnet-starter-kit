using System.Net;

namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// FullStackHero exception used for consistent error handling across the stack.
/// Includes HTTP status codes and optional detailed error messages.
/// </summary>
public class CustomException : Exception
{
    /// <summary>
    /// A list of error messages (e.g., validation errors, business rules).
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; }

    /// <summary>
    /// The HTTP status code associated with this exception.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    public CustomException(
        string message,
        IEnumerable<string>? errors = null,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }

    public CustomException(
        string message,
        Exception innerException,
        IEnumerable<string>? errors = null,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message, innerException)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }
}