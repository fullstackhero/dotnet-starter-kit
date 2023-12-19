using FSH.Framework.Infrastructure.Exceptions;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Framework.Infrastructure.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure;

public static class Extensions
{
    public static WebApplicationBuilder AddFSHFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLogging();
        builder.Services.AddOpenApi();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        return builder;
    }

    public static WebApplication UseFSHFramework(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseExceptionHandler();
        return app;
    }
}
