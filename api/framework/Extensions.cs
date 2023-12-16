using FSH.Framework.Logging;
using FSH.Framework.OpenApi;
using FSH.WebApi.Framework.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework;

public static class Extensions
{
    public static WebApplicationBuilder AddFSH(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLogging();
        builder.Services.AddOpenApi();
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.AddProblemDetails();
        return builder;
    }

    public static WebApplication UseFSH(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseExceptionHandler();
        return app;
    }
}
