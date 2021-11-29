using System.Net;

namespace DN.WebApi.Application.Common.Exceptions;

public class ValidationException : CustomException
{
    public ValidationException(List<string> errors = default)
        : base("Validation Failures Occured.", errors, HttpStatusCode.BadRequest)
    {
    }
}