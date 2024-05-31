using System.Collections.ObjectModel;
using System.Net;

namespace FSH.Framework.Core.Exceptions;
public class ForbiddenException : FshException
{
    public ForbiddenException()
        : base("not authorized", new Collection<string>(), HttpStatusCode.Forbidden)
    {
    }
    public ForbiddenException(string message)
       : base(message, new Collection<string>(), HttpStatusCode.Forbidden)
    {
    }
}
