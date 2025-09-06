using FSH.Modules.Common.Core.Exceptions;
using System.Net;

namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// Exception representing a 401 Unauthorized error (authentication failure).
/// </summary>
public class UnauthorizedException : CustomException
{
    public UnauthorizedException()
        : base("Authentication failed.", Array.Empty<string>(), HttpStatusCode.Unauthorized)
    {
    }

    public UnauthorizedException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.Unauthorized)
    {
    }

    public UnauthorizedException(string message, IEnumerable<string> errors)
        : base(message, errors.ToList(), HttpStatusCode.Unauthorized)
    {
    }
}