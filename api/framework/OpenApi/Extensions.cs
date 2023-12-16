using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace FSH.Framework.OpenApi;

public static class Extensions
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }
    public static WebApplication UseOpenApiDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.DefaultModelExpandDepth(-1);
                options.DocExpansion(DocExpansion.List);
            });
        }
        return app;
    }
}
