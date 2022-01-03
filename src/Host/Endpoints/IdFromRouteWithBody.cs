using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints;

public class IdFromRouteWithBody<TRequest>
{
    [FromRoute]
    public Guid Id { get; set; }

    [FromBody]
    public TRequest Body { get; set; } = default!;
}