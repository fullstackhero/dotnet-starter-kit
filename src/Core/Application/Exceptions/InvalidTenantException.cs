using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class InvalidTenantException : CustomException
    {
        public InvalidTenantException() : base("Please provide a valid Tenant.", null, HttpStatusCode.BadRequest)
        {
        }
    }
}