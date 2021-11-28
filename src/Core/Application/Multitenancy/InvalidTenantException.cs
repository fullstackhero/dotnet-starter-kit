using DN.WebApi.Application.Exceptions;
using System.Net;

namespace DN.WebApi.Application.Multitenancy;

public class InvalidTenantException : CustomException
{
    public InvalidTenantException(string message)
    : base(message, null, HttpStatusCode.BadRequest)
    {
    }
}