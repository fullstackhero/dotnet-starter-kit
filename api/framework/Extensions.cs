using FSH.Framework.Logging;
using FSH.Framework.OpenApi;
using FSH.WebApi.Framework.Mediator;
using FSH.WebApi.Framework.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework;

public static class Extensions
{
    public static WebApplicationBuilder AddFSH(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddVersioning();
        builder.AddLogging();
        builder.AddFSHMediator();
        builder.Services.AddOpenApiDocumentation();
        return builder;
    }

    public static WebApplication UseFSH(this WebApplication app)
    {
        app.UseOpenApiDocumentation();
        app.UseHttpsRedirection();
        return app;
    }
}
