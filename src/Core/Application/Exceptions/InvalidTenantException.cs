using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class InvalidTenantException : CustomException
    {
        public InvalidTenantException() : base("Invalid Tenant.", null, HttpStatusCode.BadRequest)
        {
        }
    }
}