using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints;

public class IdFromRoute
{
    // Has to be lowercase 'id' (or rather the same case as is used in the route templates e.g. "{id:guid}")
    [FromRoute(Name = "id")]
    public Guid Id { get; set; }
}