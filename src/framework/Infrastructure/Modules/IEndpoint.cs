using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Modules;
public interface IEndpoint
{
    RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpoints);
}
