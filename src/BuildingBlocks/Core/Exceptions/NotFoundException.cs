using System.Net;

namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// Exception representing a 404 Not Found error.
/// </summary>
public class NotFoundException : CustomException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with default message.
    /// </summary>
    public NotFoundException()
        : base("Resource not found.", Array.Empty<string>(), HttpStatusCode.NotFound)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with specified message.
    /// </summary>
    /// <param name="message">The error message describing what resource was not found.</param>
    public NotFoundException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.NotFound)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with message and error details.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="errors">Collection of detailed error messages.</param>
    public NotFoundException(string message, IEnumerable<string> errors)
        : base(message, errors.ToList(), HttpStatusCode.NotFound)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing what resource was not found.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException, HttpStatusCode.NotFound)
    {
    }
}
