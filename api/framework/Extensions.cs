using FSH.Framework.Logging;
using FSH.Framework.OpenApi;
using Microsoft.AspNetCore.Builder;

namespace FSH.Framework;

public static class Extensions
{
    public static WebApplicationBuilder AddFSHFramework(this WebApplicationBuilder builder)
    {
        builder.AddLogging();
        return builder;
    }

    public static WebApplication UseFSHFramework(this WebApplication app)
    {
        app.UseOpenApiDocumentation();
        return app;
    }
}
