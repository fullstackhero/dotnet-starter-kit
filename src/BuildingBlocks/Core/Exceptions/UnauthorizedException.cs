using System.Net;

namespace FSH.Framework.Core.Exceptions;
/// <summary>
/// Exception representing a 401 Unauthorized error (authentication failure).
/// </summary>
public class UnauthorizedException : CustomException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with default message.
    /// </summary>
    public UnauthorizedException()
        : base("Authentication failed.", Array.Empty<string>(), HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with specified message.
    /// </summary>
    /// <param name="message">The error message describing the authentication failure.</param>
    public UnauthorizedException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with message and error details.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="errors">Collection of detailed error messages.</param>
    public UnauthorizedException(string message, IEnumerable<string> errors)
        : base(message, errors.ToList(), HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with message and inner exception.
    /// </summary>
    /// <param name="message">The error message describing the authentication failure.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public UnauthorizedException(string message, Exception innerException)
        : base(message, innerException, HttpStatusCode.Unauthorized)
    {
    }
}
