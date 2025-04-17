using System.Net;

namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// Exception representing a 404 Not Found error.
/// </summary>
public class NotFoundException : CustomException
{
    public NotFoundException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string message, IEnumerable<string> errors)
        : base(message, errors.ToList(), HttpStatusCode.NotFound)
    {
    }
}