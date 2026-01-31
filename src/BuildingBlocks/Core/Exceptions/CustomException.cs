using System.Net;
using System.Linq;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with default message and internal server error status.
    /// </summary>
    public CustomException()
        : this("An error occurred.", Enumerable.Empty<string>(), HttpStatusCode.InternalServerError)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CustomException(string message)
        : this(message, Enumerable.Empty<string>(), HttpStatusCode.InternalServerError)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public CustomException(string message, Exception innerException)
        : this(message, innerException, Enumerable.Empty<string>(), HttpStatusCode.InternalServerError)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with message, errors, and status code.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="errors">Collection of detailed error messages.</param>
    /// <param name="statusCode">The HTTP status code associated with this exception.</param>
    public CustomException(
        string message,
        IEnumerable<string>? errors,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with full parameters.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <param name="errors">Collection of detailed error messages.</param>
    /// <param name="statusCode">The HTTP status code associated with this exception.</param>
    public CustomException(
        string message,
        Exception innerException,
        IEnumerable<string>? errors,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message, innerException)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with message, inner exception, and status code.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <param name="statusCode">The HTTP status code associated with this exception.</param>
    public CustomException(
        string message,
        Exception innerException,
        HttpStatusCode statusCode)
        : this(message, innerException, Enumerable.Empty<string>(), statusCode)
    {
    }
}
