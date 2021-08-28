using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class EntityNotFoundException<T> : CustomException
    {
        public EntityNotFoundException() : base($"{typeof(T).Name} Not Found.", null, HttpStatusCode.NotFound)
        {
        }
    }
}