using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class InvalidTenantException : CustomException
    {
        public InvalidTenantException(string message)
        : base(message, null, HttpStatusCode.BadRequest)
        {
        }
    }
}