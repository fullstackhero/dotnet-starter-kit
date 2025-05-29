using System.Net;

namespace FSH.Framework.Core.Exceptions;

public class CustomException : Exception
{
    public ICollection<string>? ErrorMessages { get; }

    public HttpStatusCode StatusCode { get; }

    public CustomException(string message, ICollection<string>? errors = default, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors;
        StatusCode = statusCode;
    }
}
