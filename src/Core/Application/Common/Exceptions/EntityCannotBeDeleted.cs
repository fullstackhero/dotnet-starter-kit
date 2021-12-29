using System.Net;

namespace DN.WebApi.Application.Common.Exceptions;

public class EntityCannotBeDeleted : CustomException
{
    public EntityCannotBeDeleted(string message)
    : base(message, null, HttpStatusCode.Conflict)
    {
    }
}