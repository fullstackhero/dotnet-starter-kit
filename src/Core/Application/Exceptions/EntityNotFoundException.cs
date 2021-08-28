using System.Net;

namespace DN.WebApi.Application.Exceptions
{
    public class EntityNotFoundException<T> : CustomException
    {
        public EntityNotFoundException() : base($"{typeof(T)} Not Found.", null, HttpStatusCode.NotFound)
        {
        }
    }
}