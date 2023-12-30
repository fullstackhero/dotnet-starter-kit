using FSH.Framework.Infrastructure.Exceptions;
using FSH.Framework.Infrastructure.Logging.Serilog;
using FSH.Framework.Infrastructure.Multitenancy;
using FSH.Framework.Infrastructure.OpenApi;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure;

public static class Extensions
{
    public static WebApplicationBuilder AddFshFramework(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddLogging();
        builder.AddDatabase();
        builder.Services.AddMultitenancy();
        builder.Services.AddOpenApi();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        return builder;
    }

    public static WebApplication UseFshFramework(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseMultitenancy();
        app.UseExceptionHandler();
        return app;
    }
}
