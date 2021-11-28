using System.Net;
using DN.WebApi.Application.Exceptions;

namespace DN.WebApi.Application.Multitenancy;

public class InvalidTenantException : CustomException
{
    public InvalidTenantException(string message)
    : base(message, null, HttpStatusCode.BadRequest)
    {
    }
}