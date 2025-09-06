using System.Collections.ObjectModel;
using System.Net;

namespace FSH.Framework.Core.Exceptions;
public class UnauthorizedException : FshException
{
    public UnauthorizedException()
        : base("authentication failed", new Collection<string>(), HttpStatusCode.Unauthorized)
    {
    }
    public UnauthorizedException(string message)
       : base(message, new Collection<string>(), HttpStatusCode.Unauthorized)
    {
    }
}
