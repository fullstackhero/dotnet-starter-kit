using System.Net;

namespace FSH.Framework.Core.Exceptions;
public class FshException : Exception
{
    public IEnumerable<string> ErrorMessages { get; }

    public HttpStatusCode StatusCode { get; }

    public FshException(string message, IEnumerable<string> errors, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors;
        StatusCode = statusCode;
    }

    public FshException(string message) : base(message)
    {
        ErrorMessages = new List<string>();
    }
}
