using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class EntityNotFoundException : CustomException
    {
        public EntityNotFoundException(string message)
        : base(message, null, HttpStatusCode.NotFound)
        {
        }
    }
}