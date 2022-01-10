using System.Net;

namespace DN.WebApi.Application.Common.Exceptions;

public class NotAcceptableException : CustomException
{
    public NotAcceptableException()
        : base("There are no new changes to update for this Entity.", null, HttpStatusCode.NotAcceptable)
    {
    }
}