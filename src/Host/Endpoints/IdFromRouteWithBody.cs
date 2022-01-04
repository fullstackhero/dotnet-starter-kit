using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints;

public class IdFromRouteWithBody<TRequest> : IdFromRoute
{
    [FromBody]
    public TRequest Body { get; set; } = default!;
}