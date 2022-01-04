using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Application.Common.Endpoints;

public class IdFromRouteWithBody<TRequest> : IdFromRoute
{
    [FromBody]
    public TRequest Body { get; set; } = default!;
}