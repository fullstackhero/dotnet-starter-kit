using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace FSH.Framework.Web.Modules;

public interface IModule
{
    // DI/Options/Health/etc. — don’t depend on ASP.NET types here
    void ConfigureServices(IHostApplicationBuilder builder);

    // HTTP wiring — Minimal APIs only
    void MapEndpoints(IEndpointRouteBuilder endpoints);

    // Middleware wiring — called during pipeline configuration.
    // Default implementation is a no-op so existing modules are not forced to implement this.
    void ConfigureMiddleware(IApplicationBuilder app) { }
}