using System.Net;

namespace FSH.Framework.Core.Exceptions;
/// <summary>
/// Exception representing a 403 Forbidden error.
/// </summary>
public class ForbiddenException : CustomException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class with default message.
    /// </summary>
    public ForbiddenException()
        : base("Unauthorized access.", Array.Empty<string>(), HttpStatusCode.Forbidden)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class with specified message.
    /// </summary>
    /// <param name="message">The error message describing the forbidden action.</param>
    public ForbiddenException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.Forbidden)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class with message and error details.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="errors">Collection of detailed error messages.</param>
    public ForbiddenException(string message, IEnumerable<string> errors)
        : base(message, errors.ToList(), HttpStatusCode.Forbidden)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class with message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing the forbidden action.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException, HttpStatusCode.Forbidden)
    {
    }
}
