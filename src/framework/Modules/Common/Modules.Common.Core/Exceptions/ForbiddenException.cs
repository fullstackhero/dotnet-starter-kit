using FSH.Modules.Common.Core.Exceptions;
using System.Net;

namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// Exception representing a 403 Forbidden error.
/// </summary>
public class ForbiddenException : CustomException
{
    public ForbiddenException()
        : base("Unauthorized access.", Array.Empty<string>(), HttpStatusCode.Forbidden)
    {
    }

    public ForbiddenException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.Forbidden)
    {
    }

    public ForbiddenException(string message, IEnumerable<string> errors)
        : base(message, errors.ToList(), HttpStatusCode.Forbidden)
    {
    }
}