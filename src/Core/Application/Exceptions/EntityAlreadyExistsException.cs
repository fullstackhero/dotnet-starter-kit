using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class EntityAlreadyExistsException<T> : CustomException
    {
        public EntityAlreadyExistsException(string property, string value) : base($"{typeof(T).Name} with {property} : {value} already Exists.", null, HttpStatusCode.NotFound)
        {
        }
    }
}