using System.Collections.ObjectModel;
using System.Net;

namespace FSH.Framework.Core.Exceptions;
public class UnauthorizedException : FshException
{
    public UnauthorizedException()
        : base("you are not authorized to access this resource.", new Collection<string>(), HttpStatusCode.Unauthorized)
    {
    }
    public UnauthorizedException(string message)
       : base(message, new Collection<string>(), HttpStatusCode.Unauthorized)
    {
    }
}
