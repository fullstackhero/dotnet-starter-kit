using System.Net;

namespace FSH.Framework.Core.Exceptions;
public class NotFoundException : FshException
{
    public NotFoundException(string message)
        : base(message, null, HttpStatusCode.NotFound)
    {
    }
}
