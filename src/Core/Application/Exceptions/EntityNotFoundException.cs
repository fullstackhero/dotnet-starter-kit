using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class EntityNotFoundException<T> : CustomException
    {
        public EntityNotFoundException(object entityId) : base($"{typeof(T).Name} {entityId} Not Found.", null, HttpStatusCode.NotFound)
        {
        }
    }
}