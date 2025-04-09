using FSH.Framework.Core.Modules;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Modules;
public interface IEndpointModule : ICoreModule
{
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}