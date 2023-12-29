using System.Collections.ObjectModel;
using System.Net;

namespace FSH.Framework.Core.Exceptions;
public class FshException : Exception
{
    public Collection<string> ErrorMessages { get; }

    public HttpStatusCode StatusCode { get; }

    public FshException(string message, Collection<string> errors, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors;
        StatusCode = statusCode;
    }

    public FshException(string message) : base(message)
    {
        ErrorMessages = new Collection<string>();
    }
}
